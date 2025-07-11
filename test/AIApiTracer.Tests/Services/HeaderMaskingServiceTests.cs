using AIApiTracer.Services;
using AIApiTracer.Services.Metadata;
using AIApiTracer.Services.Streaming;
using Xunit;

namespace AIApiTracer.Tests.Services;

public class HeaderMaskingServiceTests
{
    private readonly HeaderMaskingService _service = new();

    [Fact]
    public void MaskSensitiveHeaders_MasksAuthorizationHeader()
    {
        // Arrange
        var headers = new Dictionary<string, string[]>
        {
            ["Authorization"] = new[] { "Bearer sk-1234567890abcdefghij" },
            ["Content-Type"] = new[] { "application/json" }
        };

        // Act
        var masked = _service.MaskSensitiveHeaders(headers);

        // Assert
        Assert.NotEqual(headers["Authorization"][0], masked["Authorization"][0]);
        Assert.Contains("Bearer", masked["Authorization"][0]);
        Assert.Contains("*", masked["Authorization"][0]);
        Assert.Equal(headers["Content-Type"][0], masked["Content-Type"][0]);
    }

    [Fact]
    public void MaskSensitiveHeaders_MasksApiKeyHeader()
    {
        // Arrange
        var headers = new Dictionary<string, string[]>
        {
            ["api-key"] = new[] { "my-secret-api-key-12345" },
            ["x-api-key"] = new[] { "another-secret-key" }
        };

        // Act
        var masked = _service.MaskSensitiveHeaders(headers);

        // Assert
        Assert.Contains("*", masked["api-key"][0]);
        Assert.Contains("*", masked["x-api-key"][0]);
        Assert.NotEqual(headers["api-key"][0], masked["api-key"][0]);
        Assert.NotEqual(headers["x-api-key"][0], masked["x-api-key"][0]);
    }

    [Fact]
    public void MaskSensitiveHeaders_PreservesCaseInsensitivity()
    {
        // Arrange
        var headers = new Dictionary<string, string[]>
        {
            ["API-KEY"] = new[] { "my-secret-key-12345" },
            ["Api-Key"] = new[] { "another-secret-key-67890" },
            ["authorization"] = new[] { "Bearer sk-test-1234567890" }
        };

        // Act
        var masked = _service.MaskSensitiveHeaders(headers);

        // Assert
        Assert.All(masked.Values, values => Assert.Contains("*", values[0]));
    }

    [Fact]
    public void MaskValue_MasksOpenAIKey()
    {
        // Arrange
        var key = "sk-proj-abc123def456ghi789jkl";

        // Act
        var masked = _service.MaskValue("api-key", key);

        // Assert
        Assert.StartsWith("sk-pro", masked);  // First 6 chars
        Assert.Contains("*", masked);
        Assert.EndsWith("jkl", masked);  // Last 3 chars
        // Verify exact format: sk-pro + 20 asterisks + jkl (total 29 chars)
        Assert.Equal("sk-pro********************jkl", masked);
    }

    [Fact]
    public void MaskValue_MasksAnthropicKey()
    {
        // Arrange
        var key = "sk-ant-api03-abc123def456ghi789jkl";

        // Act
        var masked = _service.MaskValue("x-api-key", key);

        // Assert
        // Check that it contains asterisks
        Assert.Contains("*", masked);
        // For generic masking, first 3 and last 3 chars are shown
        Assert.StartsWith("sk-", masked);
        Assert.EndsWith("jkl", masked);
    }

    [Fact]
    public void MaskValue_MasksBearerToken()
    {
        // Arrange
        var token = "Bearer sk-1234567890abcdefghij";

        // Act
        var masked = _service.MaskValue("authorization", token);

        // Assert
        Assert.StartsWith("Bearer sk-123", masked);
        Assert.Contains("*", masked);
        Assert.EndsWith("hij", masked);
    }

    [Fact]
    public void MaskValue_HandlesShortKeys()
    {
        // Arrange
        var shortKey = "abc123";

        // Act
        var masked = _service.MaskValue("api-key", shortKey);

        // Assert
        Assert.Equal("******", masked);
    }

    [Fact]
    public void MaskValue_HandlesEmptyValue()
    {
        // Arrange & Act
        var masked = _service.MaskValue("api-key", "");

        // Assert
        Assert.Equal("", masked);
    }

    [Fact]
    public void MaskValue_HandlesNullValue()
    {
        // Arrange & Act
        var masked = _service.MaskValue("api-key", null!);

        // Assert
        Assert.Null(masked);
    }

    [Fact]
    public void MaskSensitiveHeaders_MasksMultipleSensitiveHeaders()
    {
        // Arrange
        var headers = new Dictionary<string, string[]>
        {
            ["Authorization"] = new[] { "Bearer sk-test-key" },
            ["X-API-Key"] = new[] { "my-api-key" },
            ["OpenAI-API-Key"] = new[] { "sk-openai-key" },
            ["Anthropic-API-Key"] = new[] { "sk-ant-key" },
            ["Content-Type"] = new[] { "application/json" },
            ["Accept"] = new[] { "application/json" }
        };

        // Act
        var masked = _service.MaskSensitiveHeaders(headers);

        // Assert
        Assert.Contains("*", masked["Authorization"][0]);
        Assert.Contains("*", masked["X-API-Key"][0]);
        Assert.Contains("*", masked["OpenAI-API-Key"][0]);
        Assert.Contains("*", masked["Anthropic-API-Key"][0]);
        Assert.Equal("application/json", masked["Content-Type"][0]);
        Assert.Equal("application/json", masked["Accept"][0]);
    }

    [Fact]
    public void MaskValue_ExampleFromRequirement()
    {
        // Arrange
        var key = "sk-foobarbazqux";

        // Act
        var masked = _service.MaskValue("api-key", key);

        // Assert
        // sk-foobarbazqux is matched by OpenAI regex, so first 6 and last 3 chars
        Assert.Equal("sk-foo******qux", masked);
    }
}