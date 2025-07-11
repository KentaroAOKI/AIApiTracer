using AIApiTracer.Services.MessageParsing;
using Xunit;

namespace AIApiTracer.Tests.Services.MessageParsing;

public class AnthropicMessageParserTests
{
    private readonly AnthropicMessageParser _parser = new();

    [Theory]
    [InlineData(EndpointType.Anthropic, true)]
    [InlineData(EndpointType.OpenAI, false)]
    [InlineData(EndpointType.AzureOpenAI, false)]
    [InlineData(EndpointType.xAI, false)]
    [InlineData(EndpointType.Unknown, false)]
    public void CanParse_WithVariousEndpoints_ReturnsExpectedResult(EndpointType endpointType, bool expected)
    {
        // Act
        var result = _parser.CanParse(endpointType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_RequestWithSystemAndMessages_ExtractsMessagesCorrectly()
    {
        // Arrange
        var json = """
        {
            "model": "claude-3-opus",
            "system": "You are a helpful assistant.",
            "messages": [
                {"role": "user", "content": "Hello!"},
                {"role": "assistant", "content": "Hi there!"}
            ],
            "max_tokens": 1000
        }
        """;

        // Act
        var result = _parser.Parse(json, isRequest: true);

        // Assert
        Assert.Equal(3, result.Messages.Count);
        Assert.Equal("system", result.Messages[0].Role);
        Assert.Equal("You are a helpful assistant.", result.Messages[0].Content);
        Assert.Equal("user", result.Messages[1].Role);
        Assert.Equal("Hello!", result.Messages[1].Content);
        Assert.Equal("assistant", result.Messages[2].Role);
        Assert.Equal("Hi there!", result.Messages[2].Content);
        Assert.Contains("model", result.OtherData);
        Assert.Contains("max_tokens", result.OtherData);
    }

    [Fact]
    public void Parse_RequestWithNewSystemFormat_ExtractsSystemMessageCorrectly()
    {
        // Arrange
        var json = """
        {
            "model": "claude-3-opus",
            "system": [
                {"type": "text", "text": "You are a helpful assistant."},
                {"type": "text", "text": "Always be polite."}
            ],
            "messages": [
                {"role": "user", "content": "Hello!"}
            ],
            "max_tokens": 1000
        }
        """;

        // Act
        var result = _parser.Parse(json, isRequest: true);

        // Assert
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal("system", result.Messages[0].Role);
        Assert.Equal("You are a helpful assistant.\nAlways be polite.", result.Messages[0].Content);
        Assert.NotNull(result.Messages[0].ContentParts);
        Assert.Equal(2, result.Messages[0].ContentParts.Count);
        Assert.Equal("text", result.Messages[0].ContentParts[0].Type);
        Assert.Equal("You are a helpful assistant.", result.Messages[0].ContentParts[0].Text);
        Assert.Equal("text", result.Messages[0].ContentParts[1].Type);
        Assert.Equal("Always be polite.", result.Messages[0].ContentParts[1].Text);
        Assert.Equal("user", result.Messages[1].Role);
        Assert.Equal("Hello!", result.Messages[1].Content);
    }

    [Fact]
    public void Parse_RequestWithMultimodalContent_ExtractsContentPartsCorrectly()
    {
        // Arrange
        var json = """
        {
            "model": "claude-3-opus",
            "messages": [
                {
                    "role": "user",
                    "content": [
                        {"type": "text", "text": "What's in this image?"},
                        {
                            "type": "image",
                            "source": {
                                "type": "base64",
                                "media_type": "image/jpeg",
                                "data": "iVBORw0KGgoAAAANS..."
                            }
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = _parser.Parse(json, isRequest: true);

        // Assert
        Assert.Single(result.Messages);
        var message = result.Messages[0];
        Assert.Equal("user", message.Role);
        Assert.NotNull(message.ContentParts);
        Assert.Equal(2, message.ContentParts.Count);
        Assert.Equal("text", message.ContentParts[0].Type);
        Assert.Equal("What's in this image?", message.ContentParts[0].Text);
        Assert.Equal("image", message.ContentParts[1].Type);
        Assert.StartsWith("data:image/jpeg;base64,", message.ContentParts[1].ImageUrl);
    }

    [Fact]
    public void Parse_RequestWithToolUse_ExtractsContentPartsCorrectly()
    {
        // Arrange
        var json = """
        {
            "model": "claude-3-opus",
            "messages": [
                {
                    "role": "assistant",
                    "content": [
                        {"type": "text", "text": "I'll help you with that."},
                        {
                            "type": "tool_use",
                            "id": "toolu_123",
                            "name": "get_weather",
                            "input": {"location": "Tokyo"}
                        }
                    ]
                },
                {
                    "role": "user",
                    "content": [
                        {
                            "type": "tool_result",
                            "tool_use_id": "toolu_123",
                            "content": "Sunny, 25°C"
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var result = _parser.Parse(json, isRequest: true);

        // Assert
        Assert.Equal(2, result.Messages.Count);
        
        // Check assistant message
        var assistantMsg = result.Messages[0];
        Assert.Equal("assistant", assistantMsg.Role);
        Assert.Equal(2, assistantMsg.ContentParts.Count);
        Assert.Equal("text", assistantMsg.ContentParts[0].Type);
        Assert.Equal("tool_use", assistantMsg.ContentParts[1].Type);
        
        // Check user message with tool result
        var userMsg = result.Messages[1];
        Assert.Equal("user", userMsg.Role);
        Assert.Single(userMsg.ContentParts);
        Assert.Equal("tool_result", userMsg.ContentParts[0].Type);
        Assert.Equal("Sunny, 25°C", userMsg.ContentParts[0].Text);
    }

    [Fact]
    public void Parse_ResponseWithTextContent_ExtractsMessagesCorrectly()
    {
        // Arrange
        var json = """
        {
            "id": "msg_123",
            "type": "message",
            "role": "assistant",
            "model": "claude-3-opus",
            "content": [
                {"type": "text", "text": "Hello! How can I help you today?"}
            ],
            "stop_reason": "end_turn",
            "usage": {
                "input_tokens": 10,
                "output_tokens": 20
            }
        }
        """;

        // Act
        var result = _parser.Parse(json, isRequest: false);

        // Assert
        Assert.Single(result.Messages);
        var message = result.Messages[0];
        Assert.Equal("assistant", message.Role);
        Assert.Equal("Hello! How can I help you today?", message.Content);
        Assert.NotNull(message.ContentParts);
        Assert.Single(message.ContentParts);
        Assert.Equal("text", message.ContentParts[0].Type);
        Assert.Contains("id", result.OtherData);
        Assert.Contains("model", result.OtherData);
        Assert.Contains("stop_reason", result.OtherData);
    }

    [Fact]
    public void Parse_ResponseWithToolUse_ExtractsToolCallsCorrectly()
    {
        // Arrange
        var json = """
        {
            "id": "msg_123",
            "type": "message",
            "role": "assistant",
            "model": "claude-3-opus",
            "content": [
                {"type": "text", "text": "I'll calculate that for you."},
                {
                    "type": "tool_use",
                    "id": "toolu_456",
                    "name": "calculator",
                    "input": {"operation": "add", "a": 5, "b": 3}
                }
            ],
            "stop_reason": "tool_use"
        }
        """;

        // Act
        var result = _parser.Parse(json, isRequest: false);

        // Assert
        Assert.Single(result.Messages);
        Assert.Single(result.ToolCalls);
        
        var message = result.Messages[0];
        Assert.Equal("assistant", message.Role);
        Assert.Contains("I'll calculate that for you.", message.Content);
        
        var toolCall = result.ToolCalls[0];
        Assert.Equal("toolu_456", toolCall.Id);
        Assert.Equal("calculator", toolCall.Name);
        Assert.NotNull(toolCall.Arguments);
    }

    [Fact]
    public void Parse_ResponseWithMultipleTextBlocks_CombinesTextCorrectly()
    {
        // Arrange
        var json = """
        {
            "role": "assistant",
            "content": [
                {"type": "text", "text": "First part."},
                {"type": "text", "text": "Second part."},
                {"type": "text", "text": "Third part."}
            ]
        }
        """;

        // Act
        var result = _parser.Parse(json, isRequest: false);

        // Assert
        Assert.Single(result.Messages);
        var message = result.Messages[0];
        Assert.Equal("First part.\nSecond part.\nThird part.", message.Content);
        Assert.Equal(3, message.ContentParts.Count);
    }

    [Fact]
    public void Parse_InvalidJson_ReturnsEmptyResult()
    {
        // Arrange
        var json = "invalid json";

        // Act
        var result = _parser.Parse(json, isRequest: true);

        // Assert
        Assert.Empty(result.Messages);
        Assert.Empty(result.ToolCalls);
        Assert.Empty(result.OtherData);
    }

    [Fact]
    public void Parse_EmptyJson_ReturnsEmptyResult()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = _parser.Parse(json, isRequest: true);

        // Assert
        Assert.Empty(result.Messages);
        Assert.Empty(result.ToolCalls);
        Assert.Empty(result.OtherData);
    }
}