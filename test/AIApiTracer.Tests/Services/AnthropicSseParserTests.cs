using System.Net.ServerSentEvents;
using System.Text;
using AIApiTracer.Services;
using AIApiTracer.Services.Metadata;
using AIApiTracer.Services.Streaming;

namespace AIApiTracer.Tests.Services;

public class AnthropicSseParserTests
{
    private readonly AnthropicSseParser _parser;

    public AnthropicSseParserTests()
    {
        _parser = new AnthropicSseParser();
    }

    [Fact]
    public async Task ParseStreamToMessageAsync_WithResourceFile_ReturnsCompleteMessage()
    {
        // Arrange
        var resourcePath = Path.Combine(AppContext.BaseDirectory, "Resoruces", "anthropic-v1-messages_response_streaming.txt");
        using var fileStream = File.OpenRead(resourcePath);

        // Act
        var result = await _parser.ParseStreamToMessageAsync(fileStream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("msg_015a9RiwaaTpyNo43xnE71Gh", result.Id);
        Assert.Equal("message", result.Type);
        Assert.Equal("assistant", result.Role);
        Assert.Equal("claude-opus-4-20250514", result.Model);
        
        // Check content
        Assert.Single(result.Content);
        var content = result.Content[0];
        Assert.Equal("text", content.Type);
        Assert.Equal("C# is a modern, object-oriented programming language developed by Microsoft that runs on the .NET platform.\nIt combines the power of C++ with the simplicity of Visual Basic, featuring strong typing, garbage collection, and extensive standard libraries.\nWidely used for enterprise applications, game development with Unity, web services, and cross-platform development.", content.Text);
        
        // Check stop reason
        Assert.Equal("end_turn", result.StopReason);
        Assert.Null(result.StopSequence);
        
        // Check usage
        Assert.NotNull(result.Usage);
        Assert.Equal(4, result.Usage.InputTokens);
        Assert.Equal(75, result.Usage.OutputTokens);
        Assert.Equal(1165, result.Usage.CacheCreationInputTokens);
        Assert.Equal(13024, result.Usage.CacheReadInputTokens);
        Assert.Equal("standard", result.Usage.ServiceTier);
    }

    [Fact]
    public void ParseSseEvents_WithBasicMessage_ReturnsCorrectMessage()
    {
        // Arrange
        var events = new List<SseItem<string>>
        {
            new("""{"type":"message_start","message":{"id":"msg_123","type":"message","role":"assistant","model":"claude-3","content":[],"stop_reason":null,"stop_sequence":null,"usage":{"input_tokens":10,"output_tokens":1}}}""", "message_start"),
            new("""{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}""", "content_block_start"),
            new("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hello"}}""", "content_block_delta"),
            new("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":" world!"}}""", "content_block_delta"),
            new("""{"type":"content_block_stop","index":0}""", "content_block_stop"),
            new("""{"type":"message_delta","delta":{"stop_reason":"end_turn","stop_sequence":null},"usage":{"output_tokens":5}}""", "message_delta"),
            new("""{"type":"message_stop"}""", "message_stop")
        };

        // Act
        var result = _parser.ParseSseEvents(events);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("msg_123", result.Id);
        Assert.Equal("message", result.Type);
        Assert.Equal("assistant", result.Role);
        Assert.Equal("claude-3", result.Model);
        
        Assert.Single(result.Content);
        Assert.Equal("Hello world!", result.Content[0].Text);
        
        Assert.Equal("end_turn", result.StopReason);
        Assert.NotNull(result.Usage);
        Assert.Equal(10, result.Usage.InputTokens);
        Assert.Equal(5, result.Usage.OutputTokens);
    }

    [Fact]
    public void ParseSseEvents_WithMultipleContentBlocks_MergesCorrectly()
    {
        // Arrange
        var events = new List<SseItem<string>>
        {
            new("""{"type":"message_start","message":{"id":"msg_456","type":"message","role":"assistant","model":"claude-3","content":[]}}""", "message_start"),
            new("""{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}""", "content_block_start"),
            new("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"First"}}""", "content_block_delta"),
            new("""{"type":"content_block_stop","index":0}""", "content_block_stop"),
            new("""{"type":"content_block_start","index":1,"content_block":{"type":"text","text":""}}""", "content_block_start"),
            new("""{"type":"content_block_delta","index":1,"delta":{"type":"text_delta","text":"Second"}}""", "content_block_delta"),
            new("""{"type":"content_block_stop","index":1}""", "content_block_stop"),
            new("""{"type":"message_stop"}""", "message_stop")
        };

        // Act
        var result = _parser.ParseSseEvents(events);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Content.Count);
        Assert.Equal("First", result.Content[0].Text);
        Assert.Equal("Second", result.Content[1].Text);
    }

    [Fact]
    public void ParseSseEvents_WithPingEvents_IgnoresThem()
    {
        // Arrange
        var events = new List<SseItem<string>>
        {
            new("""{"type":"message_start","message":{"id":"msg_789","type":"message","role":"assistant","model":"claude-3","content":[]}}""", "message_start"),
            new("""{"type":"ping"}""", "ping"),
            new("""{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}""", "content_block_start"),
            new("""{"type":"ping"}""", "ping"),
            new("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Test"}}""", "content_block_delta"),
            new("""{"type":"ping"}""", "ping"),
            new("""{"type":"content_block_stop","index":0}""", "content_block_stop"),
            new("""{"type":"message_stop"}""", "message_stop")
        };

        // Act
        var result = _parser.ParseSseEvents(events);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("msg_789", result.Id);
        Assert.Single(result.Content);
        Assert.Equal("Test", result.Content[0].Text);
    }

    [Fact]
    public async Task ParseStreamToMessageAsync_WithStreamContent_ParsesCorrectly()
    {
        // Arrange
        var sseContent = """
            event: message_start
            data: {"type":"message_start","message":{"id":"msg_test","type":"message","role":"assistant","model":"claude-3","content":[]}}

            event: content_block_start
            data: {"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}

            event: content_block_delta
            data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Streaming test"}}

            event: content_block_stop
            data: {"type":"content_block_stop","index":0}

            event: message_stop
            data: {"type":"message_stop"}
            """;
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sseContent));

        // Act
        var result = await _parser.ParseStreamToMessageAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("msg_test", result.Id);
        Assert.Single(result.Content);
        Assert.Equal("Streaming test", result.Content[0].Text);
    }
}