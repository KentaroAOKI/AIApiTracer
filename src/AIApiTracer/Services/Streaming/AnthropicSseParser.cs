using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;

namespace AIApiTracer.Services.Streaming;

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
        var contentBlocks = new Dictionary<int, StringBuilder>();

        foreach (var sseItem in events)
        {
            // SseItem.Data contains the data, EventType is in sseItem.Data for named events
            var eventType = sseItem.EventType;
            var data = sseItem.Data;
            
            if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(data))
                continue;

            ProcessEvent(eventType, data, message, contentBlocks);
        }

        // Finalize content blocks
        foreach (var kvp in contentBlocks.OrderBy(x => x.Key))
        {
            if (message.Content.Count <= kvp.Key)
            {
                message.Content.Add(new ContentBlock { Type = "text", Text = kvp.Value.ToString() });
            }
            else
            {
                message.Content[kvp.Key].Text = kvp.Value.ToString();
            }
        }

        return message;
    }

    private void ProcessEvent(string eventType, string dataJson, Message message, Dictionary<int, StringBuilder> contentBlocks)
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
                    if (blockStart != null)
                    {
                        contentBlocks[blockStart.Index] = new StringBuilder();
                    }
                    break;

                case "content_block_delta":
                    var blockDelta = JsonSerializer.Deserialize<ContentBlockDeltaEvent>(dataJson, JsonOptions);
                    if (blockDelta?.Delta?.Text != null && contentBlocks.ContainsKey(blockDelta.Index))
                    {
                        contentBlocks[blockDelta.Index].Append(blockDelta.Delta.Text);
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