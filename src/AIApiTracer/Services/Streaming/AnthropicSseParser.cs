using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;

namespace AIApiTracer.Services.Streaming;

internal class ContentBlockBuilder
{
    public string Type { get; set; } = string.Empty;
    public string? Id { get; set; }
    public string? Name { get; set; }
    public StringBuilder? TextBuilder { get; set; }
    public StringBuilder? JsonBuilder { get; set; }

    public ContentBlock Build()
    {
        var block = new ContentBlock
        {
            Type = Type,
            Id = Id,
            Name = Name
        };

        if (Type == "text" && TextBuilder != null)
        {
            block.Text = TextBuilder.ToString();
        }
        else if (Type == "tool_use" && JsonBuilder != null)
        {
            var jsonString = JsonBuilder.ToString();
            if (!string.IsNullOrWhiteSpace(jsonString))
            {
                try
                {
                    block.Input = JsonSerializer.Deserialize<JsonElement>(jsonString);
                }
                catch
                {
                    block.Input = jsonString;
                }
            }
        }

        return block;
    }
}

public interface IAnthropicSseParser
{
    Task<Message> ParseStreamToMessageAsync(Stream stream);
    Message ParseSseEvents(IEnumerable<SseItem<string>> events);
}

public class AnthropicSseParser : IAnthropicSseParser, ISseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Message> ParseStreamToMessageAsync(Stream stream)
    {
        var events = new List<SseItem<string>>();
        
        await foreach (var sseItem in SseParser.Create(stream).EnumerateAsync())
        {
            events.Add(sseItem);
        }

        return ParseSseEvents(events);
    }

    public Message ParseSseEvents(IEnumerable<SseItem<string>> events)
    {
        var message = new Message();
        var contentBlockBuilders = new Dictionary<int, ContentBlockBuilder>();

        foreach (var sseItem in events)
        {
            // SseItem.Data contains the data, EventType is in sseItem.Data for named events
            var eventType = sseItem.EventType;
            var data = sseItem.Data;
            
            if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(data))
                continue;

            ProcessEvent(eventType, data, message, contentBlockBuilders);
        }

        // Finalize content blocks
        foreach (var kvp in contentBlockBuilders.OrderBy(x => x.Key))
        {
            var builder = kvp.Value;
            if (message.Content.Count <= kvp.Key)
            {
                message.Content.Add(builder.Build());
            }
        }

        return message;
    }

    private void ProcessEvent(string eventType, string dataJson, Message message, Dictionary<int, ContentBlockBuilder> contentBlockBuilders)
    {
        try
        {
            switch (eventType)
            {
                case "message_start":
                    var messageStart = JsonSerializer.Deserialize<MessageStartEvent>(dataJson, JsonOptions);
                    if (messageStart?.Message != null)
                    {
                        message.Id = messageStart.Message.Id;
                        message.Type = messageStart.Message.Type;
                        message.Role = messageStart.Message.Role;
                        message.Model = messageStart.Message.Model;
                        message.Usage = messageStart.Message.Usage;
                    }
                    break;

                case "content_block_start":
                    var blockStart = JsonSerializer.Deserialize<ContentBlockStartEvent>(dataJson, JsonOptions);
                    if (blockStart?.ContentBlock != null)
                    {
                        var blockBuilder = new ContentBlockBuilder
                        {
                            Type = blockStart.ContentBlock.Type,
                            Id = blockStart.ContentBlock.Id,
                            Name = blockStart.ContentBlock.Name
                        };

                        if (blockStart.ContentBlock.Type == "text")
                        {
                            blockBuilder.TextBuilder = new StringBuilder();
                        }
                        else if (blockStart.ContentBlock.Type == "tool_use")
                        {
                            blockBuilder.JsonBuilder = new StringBuilder();
                        }

                        contentBlockBuilders[blockStart.Index] = blockBuilder;
                    }
                    break;

                case "content_block_delta":
                    var blockDelta = JsonSerializer.Deserialize<ContentBlockDeltaEvent>(dataJson, JsonOptions);
                    if (blockDelta?.Delta != null && contentBlockBuilders.TryGetValue(blockDelta.Index, out var builder))
                    {
                        if (blockDelta.Delta.Type == "text_delta" && blockDelta.Delta.Text != null && builder.TextBuilder != null)
                        {
                            builder.TextBuilder.Append(blockDelta.Delta.Text);
                        }
                        else if (blockDelta.Delta.Type == "input_json_delta" && blockDelta.Delta.PartialJson != null && builder.JsonBuilder != null)
                        {
                            builder.JsonBuilder.Append(blockDelta.Delta.PartialJson);
                        }
                    }
                    break;

                case "message_delta":
                    var messageDelta = JsonSerializer.Deserialize<MessageDeltaEvent>(dataJson, JsonOptions);
                    if (messageDelta?.Delta != null)
                    {
                        if (!string.IsNullOrEmpty(messageDelta.Delta.StopReason))
                        {
                            message.StopReason = messageDelta.Delta.StopReason;
                        }
                        if (!string.IsNullOrEmpty(messageDelta.Delta.StopSequence))
                        {
                            message.StopSequence = messageDelta.Delta.StopSequence;
                        }
                    }
                    if (messageDelta?.Usage != null)
                    {
                        // Update output tokens (cumulative)
                        if (message.Usage == null)
                        {
                            message.Usage = new Usage();
                        }
                        message.Usage.OutputTokens = messageDelta.Usage.OutputTokens;
                    }
                    break;

                case "content_block_stop":
                case "message_stop":
                case "ping":
                    // These events don't require processing for message reconstruction
                    break;
            }
        }
        catch (JsonException)
        {
            // Log parsing error if needed
        }
    }

    // ISseParser implementation
    public bool CanParse(string targetUrl)
    {
        return targetUrl.Contains("api.anthropic.com", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ParseSseStreamAsync(Stream stream)
    {
        var message = await ParseStreamToMessageAsync(stream);
        return JsonSerializer.Serialize(message, JsonOptions);
    }
}