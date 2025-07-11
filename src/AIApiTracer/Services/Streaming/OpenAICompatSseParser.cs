using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;

namespace AIApiTracer.Services.Streaming;

/// <summary>
/// SSE parser for OpenAI-compatible API responses
/// </summary>
public class OpenAICompatSseParser : ISseParser
{
    private readonly OpenAISseParser _openAiParser = new();
    
    public bool CanParse(string targetUrl)
    {
        // Only handle completions endpoints through the openai-compat proxy
        if (!targetUrl.Contains("/completions", StringComparison.OrdinalIgnoreCase))
            return false;
            
        return targetUrl.Contains("/endpoint/openai-compat/", StringComparison.OrdinalIgnoreCase);
    }
    
    public async Task<string> ParseSseStreamAsync(Stream stream)
    {
        // Delegate to the OpenAI parser logic
        return await _openAiParser.ParseSseStreamAsync(stream);
    }
}