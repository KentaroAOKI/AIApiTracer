using System.Text.Json.Serialization;

namespace AIApiTracer.Services.Streaming;

public abstract class AnthropicStreamingEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class MessageStartEvent : AnthropicStreamingEvent
{
    [JsonPropertyName("message")]
    public Message Message { get; set; } = new();
}

public class ContentBlockStartEvent : AnthropicStreamingEvent
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("content_block")]
    public ContentBlock ContentBlock { get; set; } = new();
}

public class ContentBlockDeltaEvent : AnthropicStreamingEvent
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("delta")]
    public Delta Delta { get; set; } = new();
}

public class ContentBlockStopEvent : AnthropicStreamingEvent
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
}

public class MessageDeltaEvent : AnthropicStreamingEvent
{
    [JsonPropertyName("delta")]
    public MessageDelta Delta { get; set; } = new();

    [JsonPropertyName("usage")]
    public Usage? Usage { get; set; }
}

public class MessageStopEvent : AnthropicStreamingEvent
{
}

public class PingEvent : AnthropicStreamingEvent
{
}

public class Message
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public List<ContentBlock> Content { get; set; } = new();

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }

    [JsonPropertyName("usage")]
    public Usage? Usage { get; set; }
}

public class ContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class Delta
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class MessageDelta
{
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }
}

public class Usage
{
    [JsonPropertyName("input_tokens")]
    public int? InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    [JsonPropertyName("cache_creation_input_tokens")]
    public int? CacheCreationInputTokens { get; set; }

    [JsonPropertyName("cache_read_input_tokens")]
    public int? CacheReadInputTokens { get; set; }

    [JsonPropertyName("service_tier")]
    public string? ServiceTier { get; set; }
}