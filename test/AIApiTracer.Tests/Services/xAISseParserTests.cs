using System.Text;
using AIApiTracer.Services.Streaming;
using System.Text.Json;
using Xunit;

namespace AIApiTracer.Tests.Services;

public class xAISseParserTests
{
    private readonly OpenAISseParser _parser = new();

    [Fact]
    public async Task ParseSseStreamAsync_WithxAICitations_PreservesCitationsInOutput()
    {
        // Arrange
        var sseData = @"data: {""id"":""test"",""object"":""chat.completion.chunk"",""created"":1752222504,""model"":""grok-3"",""choices"":[{""index"":0,""delta"":{""content"":""Hello"",""role"":""assistant""}}],""system_fingerprint"":""fp_123""}

data: {""id"":""test"",""object"":""chat.completion.chunk"",""created"":1752222504,""model"":""grok-3"",""choices"":[{""index"":0,""delta"":{""content"":"" world""}}],""system_fingerprint"":""fp_123""}

data: {""id"":""test"",""object"":""chat.completion.chunk"",""created"":1752222504,""model"":""grok-3"",""choices"":[{""index"":0,""delta"":{},""finish_reason"":""stop""}],""system_fingerprint"":""fp_123"",""citations"":[""https://example.invalid/1"",""https://example.invalid/2""]}

data: [DONE]
";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

        // Act
        var result = await _parser.ParseSseStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        
        // Parse the result to verify citations are preserved
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        
        // Check basic fields
        Assert.True(root.TryGetProperty("id", out var idProp));
        Assert.Equal("test", idProp.GetString());
        
        Assert.True(root.TryGetProperty("model", out var modelProp));
        Assert.Equal("grok-3", modelProp.GetString());
        
        // Check message content
        Assert.True(root.TryGetProperty("choices", out var choicesProp));
        var choices = choicesProp.EnumerateArray().ToList();
        Assert.Single(choices);
        
        var choice = choices[0];
        Assert.True(choice.TryGetProperty("message", out var messageProp));
        Assert.True(messageProp.TryGetProperty("content", out var contentProp));
        Assert.Equal("Hello world", contentProp.GetString());
        
        // Check that citations are preserved
        Assert.True(root.TryGetProperty("citations", out var citationsProp));
        Assert.Equal(JsonValueKind.Array, citationsProp.ValueKind);
        var citations = citationsProp.EnumerateArray().Select(c => c.GetString()).ToList();
        Assert.Equal(2, citations.Count);
        Assert.Contains("https://example.invalid/1", citations);
        Assert.Contains("https://example.invalid/2", citations);
    }

    [Fact]
    public async Task ParseStreamToMessageAsync_WithResourceFile_PreservesCitations()
    {
        // Arrange
        var resourcePath = Path.Combine(AppContext.BaseDirectory, "Resoruces", "xai-v1-completions_response_streaming.txt");
        
        // Skip test if resource file doesn't exist
        if (!File.Exists(resourcePath))
        {
            return;
        }
        
        using var fileStream = File.OpenRead(resourcePath);

        // Act
        var result = await _parser.ParseSseStreamAsync(fileStream);

        // Assert
        Assert.NotNull(result);
        
        // Parse the result
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        
        // Check that citations are preserved from the last chunk
        Assert.True(root.TryGetProperty("citations", out var citationsProp));
        Assert.Equal(JsonValueKind.Array, citationsProp.ValueKind);
        
        var citations = citationsProp.EnumerateArray().ToList();
        Assert.Equal(15, citations.Count); // Based on the actual file content
        
        // Verify some specific citations
        var citationUrls = citations.Select(c => c.GetString()).ToList();
        Assert.Contains("https://www.npr.org/programs/all-things-considered/2025/07/09/all-things-considered-for-july-09-2025", citationUrls);
        Assert.Contains("https://x.com/s2_underground/status/1940194451273502764", citationUrls);
    }

    [Fact]
    public async Task ParseSseStreamAsync_WithUsageAndExtraFields_PreservesAllFields()
    {
        // Arrange
        var sseData = @"data: {""id"":""test"",""object"":""chat.completion.chunk"",""created"":1752222504,""model"":""grok-3"",""choices"":[{""index"":0,""delta"":{""content"":""Test"",""role"":""assistant""}}],""custom_field"":""value1""}

data: {""id"":""test"",""object"":""chat.completion.chunk"",""created"":1752222504,""model"":""grok-3"",""choices"":[{""index"":0,""delta"":{},""finish_reason"":""stop""}],""usage"":{""prompt_tokens"":10,""completion_tokens"":5,""total_tokens"":15},""another_field"":42}

data: [DONE]
";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseData));

        // Act
        var result = await _parser.ParseSseStreamAsync(stream);

        // Assert
        Assert.NotNull(result);
        
        // Parse the result
        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        
        // Check that usage is preserved
        Assert.True(root.TryGetProperty("usage", out var usageProp));
        Assert.True(usageProp.TryGetProperty("prompt_tokens", out var promptTokensProp));
        Assert.Equal(10, promptTokensProp.GetInt32());
        
        // Check that custom fields are preserved
        Assert.True(root.TryGetProperty("custom_field", out var customFieldProp));
        Assert.Equal("value1", customFieldProp.GetString());
        
        Assert.True(root.TryGetProperty("another_field", out var anotherFieldProp));
        Assert.Equal(42, anotherFieldProp.GetInt32());
    }

    [Fact]
    public void CanParse_WithxAIUrl_ReturnsTrue()
    {
        // Arrange & Act
        var result = _parser.CanParse("https://api.x.ai/v1/chat/completions");

        // Assert
        Assert.True(result);
    }
}