using AIApiTracer.Models;

namespace AIApiTracer.Services.Metadata;

/// <summary>
/// Extracts AI metadata from OpenAI-compatible API responses
/// </summary>
public class OpenAICompatMetadataExtractor : OpenAIMetadataExtractor
{
    public override bool CanExtract(string targetUrl)
    {
        // This extractor handles any URL that was proxied through the openai-compat endpoint
        // The actual target URL could be anything, but we check for the presence of our proxy path
        return targetUrl.Contains("/endpoint/openai-compat/", StringComparison.OrdinalIgnoreCase);
    }
}