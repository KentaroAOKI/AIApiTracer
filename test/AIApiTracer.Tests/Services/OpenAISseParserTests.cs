using System.Text;
using System.Text.Json;
using AIApiTracer.Services;
using AIApiTracer.Services.Metadata;
using AIApiTracer.Services.Streaming;
using Xunit;

namespace AIApiTracer.Tests.Services;

public class OpenAISseParserTests
{
    private readonly OpenAISseParser _parser = new();

    [Theory]
    [InlineData("https://api.openai.com/v1/chat/completions", true)]
    [InlineData("https://api.openai.com/v1/completions", true)]
    [InlineData("https://myresource.openai.azure.com/openai/deployments/gpt-4/completions", true)]
    [InlineData("https://api.x.ai/v1/chat/completions", true)]
    [InlineData("https://api.anthropic.com/v1/messages", false)]
    [InlineData("https://api.openai.com/v1/embeddings", false)]
    [InlineData("https://api.openai.com/v1/images/generations", false)]
    [InlineData("https://myresource.openai.azure.com/openai/deployments/gpt-4", false)]
    public void CanParse_WithVariousUrls_ReturnsExpectedResult(string url, bool expected)
    {
        // Act
        var result = _parser.CanParse(url);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ParseSseStreamAsync_WithValidStreamData_ReturnsCombinedResponse()
    {
        // Arrange
        var sseData = "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"system_fingerprint\":\"fp_123\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\",\"content\":\"\"},\"finish_reason\":null}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"system_fingerprint\":\"fp_123\",\"choices\":[{\"index\":0,\"delta\":{\"content\":\"Hello\"},\"finish_reason\":null}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"system_fingerprint\":\"fp_123\",\"choices\":[{\"index\":0,\"delta\":{\"content\":\" world!\"},\"finish_reason\":null}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"system_fingerprint\":\"fp_123\",\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"stop\"}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"system_fingerprint\":\"fp_123\",\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":5,\"total_tokens\":15}}\n\n" +
                      "data: [DONE]\n\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

        // Act
        var result = await _parser.ParseSseStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"id\": \"chatcmpl-123\"", result);
        Assert.Contains("\"model\": \"gpt-4\"", result);
        Assert.Contains("\"content\": \"Hello world!\"", result);
        Assert.Contains("\"finish_reason\": \"stop\"", result);
        Assert.Contains("\"prompt_tokens\": 10", result);
        Assert.Contains("\"completion_tokens\": 5", result);
        Assert.Contains("\"total_tokens\": 15", result);
    }

    [Fact]
    public async Task ParseSseStreamAsync_WithCachedTokens_IncludesInResponse()
    {
        // Arrange
        var sseData = @"data: {""id"":""chatcmpl-123"",""object"":""chat.completion.chunk"",""created"":1234567890,""model"":""gpt-4"",""choices"":[{""index"":0,""delta"":{""role"":""assistant"",""content"":""Test""},""finish_reason"":null}]}

data: {""id"":""chatcmpl-123"",""object"":""chat.completion.chunk"",""created"":1234567890,""model"":""gpt-4"",""choices"":[{""index"":0,""delta"":{},""finish_reason"":""stop""}]}

data: {""id"":""chatcmpl-123"",""object"":""chat.completion.chunk"",""created"":1234567890,""model"":""gpt-4"",""usage"":{""prompt_tokens"":100,""completion_tokens"":10,""total_tokens"":110,""prompt_tokens_details"":{""cached_tokens"":80}}}

data: [DONE]";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

        // Act
        var result = await _parser.ParseSseStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"prompt_tokens\": 100", result);
        Assert.Contains("\"cached_tokens\": 80", result);
    }

    [Fact]
    public async Task ParseSseStreamAsync_WithEmptyData_ReturnsEmptyJson()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));

        // Act
        var result = await _parser.ParseSseStreamAsync(stream);

        // Assert
        Assert.Equal("{}", result);
    }

    [Fact]
    public async Task ParseSseStreamAsync_WithInvalidJson_SkipsInvalidChunks()
    {
        // Arrange
        var sseData = @"data: {""id"":""chatcmpl-123"",""object"":""chat.completion.chunk"",""created"":1234567890,""model"":""gpt-4"",""choices"":[{""index"":0,""delta"":{""content"":""Valid""},""finish_reason"":null}]}

data: {invalid json

data: {""id"":""chatcmpl-123"",""object"":""chat.completion.chunk"",""created"":1234567890,""model"":""gpt-4"",""choices"":[{""index"":0,""delta"":{""content"":"" chunk""},""finish_reason"":null}]}

data: {""id"":""chatcmpl-123"",""object"":""chat.completion.chunk"",""created"":1234567890,""model"":""gpt-4"",""choices"":[{""index"":0,""delta"":{},""finish_reason"":""stop""}]}

data: [DONE]";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

        // Act
        var result = await _parser.ParseSseStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"content\": \"Valid chunk\"", result);
    }

    [Fact]
    public async Task ParseSseStreamAsync_WithMultilineFormat_ParsesCorrectly()
    {
        // Arrange - Using actual Azure OpenAI format from test resources
        var sseData = @"data: {""choices"":[],""created"":0,""id"":"""",""model"":"""",""object"":"""",""prompt_filter_results"":[{""prompt_index"":0,""content_filter_results"":{""hate"":{""filtered"":false,""severity"":""safe""},""jailbreak"":{""filtered"":false,""detected"":false},""self_harm"":{""filtered"":false,""severity"":""safe""},""sexual"":{""filtered"":false,""severity"":""safe""},""violence"":{""filtered"":false,""severity"":""safe""}}}]}

data: {""choices"":[{""content_filter_results"":{},""delta"":{""content"":"""",""refusal"":null,""role"":""assistant""},""finish_reason"":null,""index"":0,""logprobs"":null}],""created"":1752118230,""id"":""chatcmpl-BrcHOn8iCMVIkdoTzlzbBSe0NKVir"",""model"":""gpt-4.1-mini-2025-04-14"",""object"":""chat.completion.chunk"",""system_fingerprint"":""fp_178c8d546f"",""usage"":null}

data: {""choices"":[{""content_filter_results"":{""hate"":{""filtered"":false,""severity"":""safe""},""self_harm"":{""filtered"":false,""severity"":""safe""},""sexual"":{""filtered"":false,""severity"":""safe""},""violence"":{""filtered"":false,""severity"":""safe""}},""delta"":{""content"":""C#""},""finish_reason"":null,""index"":0,""logprobs"":null}],""created"":1752118230,""id"":""chatcmpl-BrcHOn8iCMVIkdoTzlzbBSe0NKVir"",""model"":""gpt-4.1-mini-2025-04-14"",""object"":""chat.completion.chunk"",""system_fingerprint"":""fp_178c8d546f"",""usage"":null}

data: {""choices"":[{""content_filter_results"":{""hate"":{""filtered"":false,""severity"":""safe""},""self_harm"":{""filtered"":false,""severity"":""safe""},""sexual"":{""filtered"":false,""severity"":""safe""},""violence"":{""filtered"":false,""severity"":""safe""}},""delta"":{""content"":"" is""},""finish_reason"":null,""index"":0,""logprobs"":null}],""created"":1752118230,""id"":""chatcmpl-BrcHOn8iCMVIkdoTzlzbBSe0NKVir"",""model"":""gpt-4.1-mini-2025-04-14"",""object"":""chat.completion.chunk"",""system_fingerprint"":""fp_178c8d546f"",""usage"":null}

data: {""choices"":[{""content_filter_results"":{},""delta"":{},""finish_reason"":""stop"",""index"":0,""logprobs"":null}],""created"":1752118230,""id"":""chatcmpl-BrcHOn8iCMVIkdoTzlzbBSe0NKVir"",""model"":""gpt-4.1-mini-2025-04-14"",""object"":""chat.completion.chunk"",""system_fingerprint"":""fp_178c8d546f"",""usage"":null}

data: {""choices"":[],""created"":1752118230,""id"":""chatcmpl-BrcHOn8iCMVIkdoTzlzbBSe0NKVir"",""model"":""gpt-4.1-mini-2025-04-14"",""object"":""chat.completion.chunk"",""system_fingerprint"":""fp_178c8d546f"",""usage"":{""completion_tokens"":3,""completion_tokens_details"":{""accepted_prediction_tokens"":0,""audio_tokens"":0,""reasoning_tokens"":0,""rejected_prediction_tokens"":0},""prompt_tokens"":10,""prompt_tokens_details"":{""audio_tokens"":0,""cached_tokens"":0},""total_tokens"":13}}

data: [DONE]";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

        // Act
        var result = await _parser.ParseSseStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        
        // Parse and check structure
        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        
        // Check that we got the model from a later chunk
        Assert.True(root.TryGetProperty("model", out var modelProp));
        var model = modelProp.GetString();
        Assert.Equal("gpt-4.1-mini-2025-04-14", model);
        
        // Check content
        Assert.True(root.TryGetProperty("choices", out var choices));
        Assert.True(choices.GetArrayLength() > 0);
        var message = choices[0].GetProperty("message");
        Assert.Equal("C# is", message.GetProperty("content").GetString());
        
        // Check usage
        Assert.True(root.TryGetProperty("usage", out var usage));
        Assert.Equal(10, usage.GetProperty("prompt_tokens").GetInt32());
        Assert.Equal(3, usage.GetProperty("completion_tokens").GetInt32());
        
        // Check that Azure OpenAI specific properties are preserved
        Assert.True(root.TryGetProperty("prompt_filter_results", out _));
    }

    [Fact]
    public async Task ParseSseStreamAsync_WithMultipleChoices_PreservesAllChoices()
    {
        // Arrange
        var sseData = "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\",\"content\":\"\"},\"finish_reason\":null},{\"index\":1,\"delta\":{\"role\":\"assistant\",\"content\":\"\"},\"finish_reason\":null}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"choices\":[{\"index\":0,\"delta\":{\"content\":\"Hello\"},\"finish_reason\":null}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"choices\":[{\"index\":1,\"delta\":{\"content\":\"Bonjour\"},\"finish_reason\":null}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"choices\":[{\"index\":0,\"delta\":{\"content\":\" world!\"},\"finish_reason\":null}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"choices\":[{\"index\":1,\"delta\":{\"content\":\" monde!\"},\"finish_reason\":null}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"stop\"},{\"index\":1,\"delta\":{},\"finish_reason\":\"stop\"}]}\n\n" +
                      "data: [DONE]\n\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

        // Act
        var result = await _parser.ParseSseStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        
        Assert.True(root.TryGetProperty("choices", out var choices));
        Assert.Equal(2, choices.GetArrayLength());
        
        var choice0 = choices[0];
        Assert.Equal(0, choice0.GetProperty("index").GetInt32());
        Assert.Equal("Hello world!", choice0.GetProperty("message").GetProperty("content").GetString());
        Assert.Equal("stop", choice0.GetProperty("finish_reason").GetString());
        
        var choice1 = choices[1];
        Assert.Equal(1, choice1.GetProperty("index").GetInt32());
        Assert.Equal("Bonjour monde!", choice1.GetProperty("message").GetProperty("content").GetString());
        Assert.Equal("stop", choice1.GetProperty("finish_reason").GetString());
    }

    [Fact]
    public async Task ParseSseStreamAsync_WithExtendedProperties_PreservesAllProperties()
    {
        // Arrange
        var sseData = "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"custom_field\":\"custom_value\",\"service_tier\":\"premium\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\",\"content\":\"\",\"custom_delta\":true},\"logprobs\":{\"content\":[]},\"finish_reason\":null}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"choices\":[{\"index\":0,\"delta\":{\"content\":\"Test\"},\"finish_reason\":null}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"stop\"}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":1,\"total_tokens\":11}}\n\n" +
                      "data: [DONE]\n\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

        // Act
        var result = await _parser.ParseSseStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        
        // Check extended properties at root level
        Assert.Equal("custom_value", root.GetProperty("custom_field").GetString());
        Assert.Equal("premium", root.GetProperty("service_tier").GetString());
        
        // Check extended properties in choice
        var choice = root.GetProperty("choices")[0];
        Assert.True(choice.TryGetProperty("logprobs", out _));
        
        // Check extended properties in message (from delta)
        var message = choice.GetProperty("message");
        Assert.True(message.TryGetProperty("custom_delta", out var customDelta));
        Assert.True(customDelta.GetBoolean());
    }

    [Fact]
    public async Task ParseSseStreamAsync_WithAzureOpenAIFormat_HandlesContentFilterResults()
    {
        // Arrange
        var sseData = "data: {\"choices\":[],\"created\":0,\"id\":\"\",\"model\":\"\",\"object\":\"\",\"prompt_filter_results\":[{\"prompt_index\":0,\"content_filter_results\":{\"hate\":{\"filtered\":false,\"severity\":\"safe\"}}}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"choices\":[{\"index\":0,\"content_filter_results\":{\"hate\":{\"filtered\":false,\"severity\":\"safe\"}},\"delta\":{\"role\":\"assistant\",\"content\":\"Test\"},\"finish_reason\":null}]}\n\n" +
                      "data: {\"id\":\"chatcmpl-123\",\"object\":\"chat.completion.chunk\",\"created\":1234567890,\"model\":\"gpt-4\",\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"stop\"}]}\n\n" +
                      "data: [DONE]\n\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

        // Act
        var result = await _parser.ParseSseStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        
        // Check that prompt_filter_results is preserved
        Assert.True(root.TryGetProperty("prompt_filter_results", out _));
        
        // Check that content_filter_results is preserved in choice
        var choice = root.GetProperty("choices")[0];
        Assert.True(choice.TryGetProperty("content_filter_results", out _));
    }
}