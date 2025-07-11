using AIApiTracer.Services;
using AIApiTracer.Services.Metadata;
using AIApiTracer.Services.Streaming;
using Xunit;

namespace AIApiTracer.Tests.Services;

public class SseParserFactoryTests
{
    [Fact]
    public void GetParser_WithAnthropicUrl_ReturnsAnthropicParser()
    {
        // Arrange
        var parsers = new ISseParser[] { new AnthropicSseParser() };
        var factory = new SseParserFactory(parsers);

        // Act
        var parser = factory.GetParser("https://api.anthropic.com/v1/messages");

        // Assert
        Assert.NotNull(parser);
        Assert.IsType<AnthropicSseParser>(parser);
    }

    [Fact]
    public void GetParser_WithUnknownUrl_ReturnsNull()
    {
        // Arrange
        var parsers = new ISseParser[] { new AnthropicSseParser() };
        var factory = new SseParserFactory(parsers);

        // Act
        var parser = factory.GetParser("https://api.openai.com/v1/chat/completions");

        // Assert
        Assert.Null(parser);
    }

    [Fact]
    public void GetParser_WithEmptyUrl_ReturnsNull()
    {
        // Arrange
        var parsers = new ISseParser[] { new AnthropicSseParser() };
        var factory = new SseParserFactory(parsers);

        // Act
        var parser = factory.GetParser("");

        // Assert
        Assert.Null(parser);
    }

    [Fact]
    public void GetParser_WithNullUrl_ReturnsNull()
    {
        // Arrange
        var parsers = new ISseParser[] { new AnthropicSseParser() };
        var factory = new SseParserFactory(parsers);

        // Act
        var parser = factory.GetParser(null!);

        // Assert
        Assert.Null(parser);
    }

    [Fact]
    public void GetParser_WithMultipleParsers_ReturnsCorrectParser()
    {
        // Arrange
        var parsers = new ISseParser[] 
        { 
            new AnthropicSseParser(),
            new MockSseParser("api.openai.com")
        };
        var factory = new SseParserFactory(parsers);

        // Act
        var anthropicParser = factory.GetParser("https://api.anthropic.com/v1/messages");
        var openaiParser = factory.GetParser("https://api.openai.com/v1/chat/completions");

        // Assert
        Assert.NotNull(anthropicParser);
        Assert.IsType<AnthropicSseParser>(anthropicParser);
        Assert.NotNull(openaiParser);
        Assert.IsType<MockSseParser>(openaiParser);
    }

    private class MockSseParser : ISseParser
    {
        private readonly string _expectedHost;

        public MockSseParser(string expectedHost)
        {
            _expectedHost = expectedHost;
        }

        public bool CanParse(string targetUrl)
        {
            return targetUrl.Contains(_expectedHost, StringComparison.OrdinalIgnoreCase);
        }

        public Task<string> ParseSseStreamAsync(Stream stream)
        {
            return Task.FromResult("{}");
        }
    }
}