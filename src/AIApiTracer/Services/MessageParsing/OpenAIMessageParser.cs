using System.Text.Json;

namespace AIApiTracer.Services.MessageParsing;

/// <summary>
/// Parser for OpenAI API message format
/// </summary>
public class OpenAIMessageParser : BaseMessageParser
{
    private static readonly HashSet<string> KnownRequestFields = new()
    {
        "messages"
    };

    private static readonly HashSet<string> KnownResponseFields = new()
    {
        "choices"
    };

    public override bool CanParse(EndpointType endpointType)
    {
        return endpointType == EndpointType.OpenAI ||
               endpointType == EndpointType.AzureOpenAI ||
               endpointType == EndpointType.xAI ||
               endpointType == EndpointType.OpenAICompat;
    }

    public override ParsedMessageData Parse(string json, bool isRequest)
    {
        var result = new ParsedMessageData();

        using var document = TryParseJson(json);
        if (document == null)
            return result;

        var root = document.RootElement;

        if (isRequest)
        {
            ParseRequest(root, result);
        }
        else
        {
            ParseResponse(root, result);
        }

        return result;
    }

    private void ParseRequest(JsonElement root, ParsedMessageData result)
    {
        // Parse messages
        if (root.TryGetProperty("messages", out var messagesElement))
        {
            foreach (var messageElement in messagesElement.EnumerateArray())
            {
                var message = ParseMessage(messageElement);
                if (message != null)
                {
                    // Check for tool calls in assistant messages
                    if (messageElement.TryGetProperty("tool_calls", out var toolCallsElement))
                    {
                        message.ToolCalls = new List<ParsedToolCall>();
                        foreach (var toolCallElement in toolCallsElement.EnumerateArray())
                        {
                            var toolCall = ParseToolCall(toolCallElement);
                            if (toolCall != null)
                            {
                                message.ToolCalls.Add(toolCall);
                                result.ToolCalls.Add(toolCall); // Keep in result for backward compatibility
                            }
                        }
                    }
                    
                    result.Messages.Add(message);
                }
            }
        }

        // Extract other data
        result.OtherData = ExtractOtherData(root, KnownRequestFields);
    }

    private void ParseResponse(JsonElement root, ParsedMessageData result)
    {
        // Parse choices
        if (root.TryGetProperty("choices", out var choicesElement))
        {
            foreach (var choiceElement in choicesElement.EnumerateArray())
            {
                // Parse message in choice
                if (choiceElement.TryGetProperty("message", out var messageElement))
                {
                    var message = ParseMessage(messageElement);
                    if (message != null)
                    {
                        // Check for tool calls
                        if (messageElement.TryGetProperty("tool_calls", out var toolCallsElement))
                        {
                            message.ToolCalls = new List<ParsedToolCall>();
                            foreach (var toolCallElement in toolCallsElement.EnumerateArray())
                            {
                                var toolCall = ParseToolCall(toolCallElement);
                                if (toolCall != null)
                                {
                                    message.ToolCalls.Add(toolCall);
                                    result.ToolCalls.Add(toolCall); // Keep in result for backward compatibility
                                }
                            }
                        }
                        
                        result.Messages.Add(message);
                    }
                }

                // Parse delta (for streaming responses)
                if (choiceElement.TryGetProperty("delta", out var deltaElement))
                {
                    var message = ParseMessage(deltaElement);
                    if (message != null)
                    {
                        // Check for tool calls in delta
                        if (deltaElement.TryGetProperty("tool_calls", out var toolCallsElement))
                        {
                            message.ToolCalls = new List<ParsedToolCall>();
                            foreach (var toolCallElement in toolCallsElement.EnumerateArray())
                            {
                                var toolCall = ParseToolCall(toolCallElement);
                                if (toolCall != null)
                                {
                                    message.ToolCalls.Add(toolCall);
                                    result.ToolCalls.Add(toolCall); // Keep in result for backward compatibility
                                }
                            }
                        }
                        
                        result.Messages.Add(message);
                    }
                }
            }
        }

        // Extract other data
        result.OtherData = ExtractOtherData(root, KnownResponseFields);
    }

    private ParsedMessage? ParseMessage(JsonElement messageElement)
    {
        var message = new ParsedMessage();

        // Get role
        if (messageElement.TryGetProperty("role", out var roleElement))
        {
            message.Role = roleElement.GetString() ?? "unknown";
        }

        // Get content (can be string or array)
        if (messageElement.TryGetProperty("content", out var contentElement))
        {
            if (contentElement.ValueKind == JsonValueKind.String)
            {
                message.Content = contentElement.GetString();
            }
            else if (contentElement.ValueKind == JsonValueKind.Array)
            {
                message.ContentParts = new List<ContentPart>();
                foreach (var partElement in contentElement.EnumerateArray())
                {
                    var part = ParseContentPart(partElement);
                    if (part != null)
                    {
                        message.ContentParts.Add(part);
                    }
                }
            }
        }

        return message;
    }

    private ContentPart? ParseContentPart(JsonElement partElement)
    {
        var part = new ContentPart();

        if (partElement.TryGetProperty("type", out var typeElement))
        {
            part.Type = typeElement.GetString() ?? "text";
        }

        if (partElement.TryGetProperty("text", out var textElement))
        {
            part.Text = textElement.GetString();
        }

        if (partElement.TryGetProperty("image_url", out var imageUrlElement))
        {
            if (imageUrlElement.TryGetProperty("url", out var urlElement))
            {
                part.ImageUrl = urlElement.GetString();
            }
        }

        // Collect other properties
        var knownPartFields = new HashSet<string> { "type", "text", "image_url" };
        var otherData = new Dictionary<string, JsonElement>();
        foreach (var property in partElement.EnumerateObject())
        {
            if (!knownPartFields.Contains(property.Name))
            {
                otherData[property.Name] = property.Value.Clone();
            }
        }
        if (otherData.Count > 0)
        {
            part.OtherData = otherData;
        }

        return part;
    }

    private ParsedToolCall? ParseToolCall(JsonElement toolCallElement)
    {
        var toolCall = new ParsedToolCall();

        if (toolCallElement.TryGetProperty("id", out var idElement))
        {
            toolCall.Id = idElement.GetString() ?? "";
        }

        if (toolCallElement.TryGetProperty("type", out var typeElement))
        {
            toolCall.Type = typeElement.GetString() ?? "function";
        }

        if (toolCallElement.TryGetProperty("function", out var functionElement))
        {
            if (functionElement.TryGetProperty("name", out var nameElement))
            {
                toolCall.Name = nameElement.GetString() ?? "";
            }

            if (functionElement.TryGetProperty("arguments", out var argsElement))
            {
                toolCall.Arguments = argsElement.Clone();
            }
        }

        return toolCall;
    }
}