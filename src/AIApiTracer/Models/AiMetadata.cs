namespace AIApiTracer.Models;

/// <summary>
/// Represents metadata extracted from AI API responses
/// </summary>
public class AiMetadata
{
    /// <summary>
    /// The AI model used for the request
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Token usage information
    /// </summary>
    public TokenUsage? Usage { get; set; }

    /// <summary>
    /// Rate limit information
    /// </summary>
    public RateLimitInfo? RateLimit { get; set; }

    /// <summary>
    /// Additional provider-specific metadata
    /// </summary>
    public Dictionary<string, object> Extra { get; set; } = new();
}

/// <summary>
/// Token usage information from AI API responses
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Number of tokens in the prompt/input
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// Number of tokens in the completion/output
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// Total number of tokens used
    /// </summary>
    public int? TotalTokens { get; set; }

    /// <summary>
    /// Number of cached tokens (for providers that support caching)
    /// </summary>
    public int? CachedTokens { get; set; }

    /// <summary>
    /// Number of cached input tokens (Anthropic specific)
    /// </summary>
    public int? InputTokensCached { get; set; }

    /// <summary>
    /// Number of cached output tokens (Anthropic specific)
    /// </summary>
    public int? OutputTokensCached { get; set; }
    
    /// <summary>
    /// Number of cache creation input tokens (Anthropic)
    /// </summary>
    public int? CacheCreationInputTokens { get; set; }
    
    /// <summary>
    /// Number of cache read input tokens (Anthropic)
    /// </summary>
    public int? CacheReadInputTokens { get; set; }
}

/// <summary>
/// Rate limit information from AI API responses
/// </summary>
public class RateLimitInfo
{
    /// <summary>
    /// Remaining requests in the rate limit window
    /// </summary>
    public int? RemainingRequests { get; set; }

    /// <summary>
    /// Total request limit in the rate limit window
    /// </summary>
    public int? LimitRequests { get; set; }

    /// <summary>
    /// Remaining tokens in the rate limit window
    /// </summary>
    public int? RemainingTokens { get; set; }

    /// <summary>
    /// Total token limit in the rate limit window
    /// </summary>
    public int? LimitTokens { get; set; }

    /// <summary>
    /// Unix timestamp (seconds, UTC) when the rate limit will reset
    /// </summary>
    public long? ResetTimestamp { get; set; }

    /// <summary>
    /// Gets the reset time as a DateTime in UTC
    /// </summary>
    public DateTime? ResetTimeUtc => ResetTimestamp.HasValue 
        ? DateTimeOffset.FromUnixTimeSeconds(ResetTimestamp.Value).UtcDateTime 
        : null;
}