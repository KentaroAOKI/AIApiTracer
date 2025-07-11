using System.Text.Json;

namespace AIApiTracer.Services.MessageParsing;

/// <summary>
/// Parser for Anthropic API message format
/// </summary>
public class AnthropicMessageParser : BaseMessageParser
{
    private static readonly HashSet<string> KnownRequestFields = new()
    {
        "messages", "system"
    };

    private static readonly HashSet<string> KnownResponseFields = new()
    {
        "content", "role"
    };

    public override bool CanParse(EndpointType endpointType)
    {
        return endpointType == EndpointType.Anthropic;
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
        // Parse system message if present
        if (root.TryGetProperty("system", out var systemElement))
        {
            if (systemElement.ValueKind == JsonValueKind.String)
            {
                // Legacy string format
                result.Messages.Add(new ParsedMessage
                {
                    Role = "system",
                    Content = systemElement.GetString()
                });
            }
            else if (systemElement.ValueKind == JsonValueKind.Array)
            {
                // New array format with content blocks
                var systemMessage = new ParsedMessage
                {
                    Role = "system",
                    ContentParts = new List<ContentPart>()
                };
                
                foreach (var contentBlock in systemElement.EnumerateArray())
                {
                    if (contentBlock.TryGetProperty("type", out var typeElement) && 
                        typeElement.GetString() == "text" &&
                        contentBlock.TryGetProperty("text", out var textElement))
                    {
                        systemMessage.ContentParts.Add(new ContentPart
                        {
                            Type = "text",
                            Text = textElement.GetString()
                        });
                    }
                }
                
                // If we have text parts, combine them into content for convenience
                var textParts = systemMessage.ContentParts.Where(p => !string.IsNullOrEmpty(p.Text)).Select(p => p.Text).ToList();
                if (textParts.Any())
                {
                    systemMessage.Content = string.Join("\n", textParts);
                }
                
                result.Messages.Add(systemMessage);
            }
        }

        // Parse messages
        if (root.TryGetProperty("messages", out var messagesElement))
        {
            foreach (var messageElement in messagesElement.EnumerateArray())
            {
                var message = ParseMessage(messageElement);
                if (message != null)
                {
                    result.Messages.Add(message);
                }
            }
        }

        // Extract other data
        result.OtherData = ExtractOtherData(root, KnownRequestFields);
    }

    private void ParseResponse(JsonElement root, ParsedMessageData result)
    {
        // Parse content array (Anthropic response has content as an array at the root level)
        if (root.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.Array)
        {
            var message = new ParsedMessage
            {
                Role = root.TryGetProperty("role", out var roleElement) ? roleElement.GetString() ?? "assistant" : "assistant"
            };

            message.ContentParts = new List<ContentPart>();

            foreach (var contentBlock in contentElement.EnumerateArray())
            {
                if (contentBlock.TryGetProperty("type", out var typeElement))
                {
                    var type = typeElement.GetString();

                    if (type == "text")
                    {
                        var part = new ContentPart
                        {
                            Type = "text",
                            Text = contentBlock.TryGetProperty("text", out var textElement) ? textElement.GetString() : null
                        };
                        message.ContentParts.Add(part);
                    }
                    else if (type == "tool_use")
                    {
                        var toolCall = ParseToolUse(contentBlock);
                        if (toolCall != null)
                        {
                            result.ToolCalls.Add(toolCall);
                        }
                    }
                    else
                    {
                        // Other content types
                        var part = new ContentPart
                        {
                            Type = type ?? "unknown",
                            OtherData = new Dictionary<string, JsonElement>()
                        };
                        
                        foreach (var prop in contentBlock.EnumerateObject())
                        {
                            if (prop.Name != "type")
                            {
                                part.OtherData[prop.Name] = prop.Value.Clone();
                            }
                        }
                        
                        message.ContentParts.Add(part);
                    }
                }
            }

            // If we have text parts, combine them into a single content string for convenience
            var textParts = message.ContentParts.Where(p => p.Type == "text" && !string.IsNullOrEmpty(p.Text)).ToList();
            if (textParts.Count == 1)
            {
                message.Content = textParts[0].Text;
            }
            else if (textParts.Count > 1)
            {
                message.Content = string.Join("\n", textParts.Select(p => p.Text));
            }

            result.Messages.Add(message);
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

        // Get content (can be string or array of content blocks)
        if (messageElement.TryGetProperty("content", out var contentElement))
        {
            if (contentElement.ValueKind == JsonValueKind.String)
            {
                message.Content = contentElement.GetString();
            }
            else if (contentElement.ValueKind == JsonValueKind.Array)
            {
                message.ContentParts = new List<ContentPart>();
                var textParts = new List<string>();

                foreach (var contentBlock in contentElement.EnumerateArray())
                {
                    if (contentBlock.TryGetProperty("type", out var typeElement))
                    {
                        var type = typeElement.GetString();

                        if (type == "text")
                        {
                            var part = new ContentPart
                            {
                                Type = "text",
                                Text = contentBlock.TryGetProperty("text", out var textElement) ? textElement.GetString() : null
                            };
                            message.ContentParts.Add(part);
                            
                            if (!string.IsNullOrEmpty(part.Text))
                            {
                                textParts.Add(part.Text);
                            }
                        }
                        else if (type == "image")
                        {
                            var part = ParseImageContent(contentBlock);
                            if (part != null)
                            {
                                message.ContentParts.Add(part);
                            }
                        }
                        else if (type == "tool_use")
                        {
                            // Tool use in request messages
                            var toolCall = ParseToolUse(contentBlock);
                            if (toolCall != null)
                            {
                                // Store tool use info in content parts for request context
                                var part = new ContentPart
                                {
                                    Type = "tool_use",
                                    OtherData = new Dictionary<string, JsonElement>
                                    {
                                        ["id"] = JsonDocument.Parse($"\"{toolCall.Id}\"").RootElement,
                                        ["name"] = JsonDocument.Parse($"\"{toolCall.Name}\"").RootElement
                                    }
                                };
                                
                                if (toolCall.Arguments != null)
                                {
                                    part.OtherData["input"] = toolCall.Arguments.Value.Clone();
                                }
                                
                                message.ContentParts.Add(part);
                            }
                        }
                        else if (type == "tool_result")
                        {
                            // Tool result in request messages
                            var part = ParseToolResult(contentBlock);
                            if (part != null)
                            {
                                message.ContentParts.Add(part);
                            }
                        }
                    }
                }

                // Combine text parts for convenience
                if (textParts.Count > 0)
                {
                    message.Content = string.Join("\n", textParts);
                }
            }
        }

        return message;
    }

    private ContentPart? ParseImageContent(JsonElement imageBlock)
    {
        var part = new ContentPart
        {
            Type = "image"
        };

        if (imageBlock.TryGetProperty("source", out var sourceElement))
        {
            if (sourceElement.TryGetProperty("type", out var sourceTypeElement) &&
                sourceTypeElement.GetString() == "base64" &&
                sourceElement.TryGetProperty("media_type", out var mediaTypeElement) &&
                sourceElement.TryGetProperty("data", out var dataElement))
            {
                var mediaType = mediaTypeElement.GetString();
                var data = dataElement.GetString();
                part.ImageUrl = $"data:{mediaType};base64,{data}";
            }
        }

        return part;
    }

    private ParsedToolCall? ParseToolUse(JsonElement toolUseBlock)
    {
        var toolCall = new ParsedToolCall
        {
            Type = "function"
        };

        if (toolUseBlock.TryGetProperty("id", out var idElement))
        {
            toolCall.Id = idElement.GetString() ?? "";
        }

        if (toolUseBlock.TryGetProperty("name", out var nameElement))
        {
            toolCall.Name = nameElement.GetString() ?? "";
        }

        if (toolUseBlock.TryGetProperty("input", out var inputElement))
        {
            toolCall.Arguments = inputElement.Clone();
        }

        return toolCall;
    }

    private ContentPart? ParseToolResult(JsonElement toolResultBlock)
    {
        var part = new ContentPart
        {
            Type = "tool_result",
            OtherData = new Dictionary<string, JsonElement>()
        };

        if (toolResultBlock.TryGetProperty("tool_use_id", out var toolUseIdElement))
        {
            part.OtherData["tool_use_id"] = toolUseIdElement.Clone();
        }

        if (toolResultBlock.TryGetProperty("content", out var contentElement))
        {
            if (contentElement.ValueKind == JsonValueKind.String)
            {
                part.Text = contentElement.GetString();
            }
            else
            {
                part.OtherData["content"] = contentElement.Clone();
            }
        }

        if (toolResultBlock.TryGetProperty("is_error", out var isErrorElement))
        {
            part.OtherData["is_error"] = isErrorElement.Clone();
        }

        return part;
    }
}