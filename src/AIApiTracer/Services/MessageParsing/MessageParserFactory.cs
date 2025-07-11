namespace AIApiTracer.Services.MessageParsing;

/// <summary>
/// Factory for creating message parsers based on endpoint type
/// </summary>
public class MessageParserFactory : IMessageParserFactory
{
    private readonly List<IMessageParser> _parsers;

    public MessageParserFactory()
    {
        _parsers = new List<IMessageParser>
        {
            new OpenAIMessageParser(),
            new AnthropicMessageParser()
        };
    }

    public IMessageParser? GetParser(EndpointType endpointType)
    {
        return _parsers.FirstOrDefault(p => p.CanParse(endpointType));
    }

    public IMessageParser? GetParser(string targetUrl)
    {
        var endpointType = DetermineEndpointType(targetUrl);
        return GetParser(endpointType);
    }

    private EndpointType DetermineEndpointType(string targetUrl)
    {
        if (targetUrl.Contains("api.openai.com", StringComparison.OrdinalIgnoreCase))
            return EndpointType.OpenAI;
        
        if (targetUrl.Contains("openai.azure.com", StringComparison.OrdinalIgnoreCase))
            return EndpointType.AzureOpenAI;
        
        if (targetUrl.Contains("api.anthropic.com", StringComparison.OrdinalIgnoreCase))
            return EndpointType.Anthropic;
        
        if (targetUrl.Contains("api.x.ai", StringComparison.OrdinalIgnoreCase))
            return EndpointType.xAI;
        
        // Check if it's an OpenAI-compatible endpoint
        if (targetUrl.Contains("/v1/chat/completions", StringComparison.OrdinalIgnoreCase) ||
            targetUrl.Contains("/v1/completions", StringComparison.OrdinalIgnoreCase))
            return EndpointType.OpenAICompat;
        
        return EndpointType.Unknown;
    }
}

/// <summary>
/// Interface for message parser factory
/// </summary>
public interface IMessageParserFactory
{
    IMessageParser? GetParser(EndpointType endpointType);
    IMessageParser? GetParser(string targetUrl);
}