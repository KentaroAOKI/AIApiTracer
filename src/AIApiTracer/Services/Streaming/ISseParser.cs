namespace AIApiTracer.Services.Streaming;

/// <summary>
/// Interface for parsing Server-Sent Events (SSE) streams into a single merged response
/// </summary>
public interface ISseParser
{
    /// <summary>
    /// Determines if this parser can handle the given endpoint
    /// </summary>
    bool CanParse(string targetUrl);

    /// <summary>
    /// Parses SSE stream and returns merged JSON content
    /// </summary>
    Task<string> ParseSseStreamAsync(Stream stream);
}