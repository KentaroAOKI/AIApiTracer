namespace AIApiTracer.Services.Metadata;

/// <summary>
/// Factory for selecting the appropriate AI metadata extractor
/// </summary>
public interface IAiMetadataExtractorFactory
{
    /// <summary>
    /// Gets the appropriate extractor for the given endpoint
    /// </summary>
    IAiMetadataExtractor? GetExtractor(string targetUrl);
    
    /// <summary>
    /// Gets all registered extractors
    /// </summary>
    IEnumerable<IAiMetadataExtractor> GetAllExtractors();
}

public class AiMetadataExtractorFactory : IAiMetadataExtractorFactory
{
    private readonly IEnumerable<IAiMetadataExtractor> _extractors;

    public AiMetadataExtractorFactory(IEnumerable<IAiMetadataExtractor> extractors)
    {
        _extractors = extractors;
    }

    public IAiMetadataExtractor? GetExtractor(string targetUrl)
    {
        if (string.IsNullOrEmpty(targetUrl))
            return null;

        return _extractors.FirstOrDefault(e => e.CanExtract(targetUrl));
    }
    
    public IEnumerable<IAiMetadataExtractor> GetAllExtractors()
    {
        return _extractors;
    }
}