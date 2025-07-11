namespace AIApiTracer.Services.Streaming;

public interface ISseParserFactory
{
    ISseParser? GetParser(string targetUrl);
    IEnumerable<ISseParser> GetAllParsers();
}

public class SseParserFactory : ISseParserFactory
{
    private readonly IEnumerable<ISseParser> _parsers;

    public SseParserFactory(IEnumerable<ISseParser> parsers)
    {
        _parsers = parsers;
    }

    public ISseParser? GetParser(string targetUrl)
    {
        if (string.IsNullOrEmpty(targetUrl))
            return null;

        return _parsers.FirstOrDefault(p => p.CanParse(targetUrl));
    }
    
    public IEnumerable<ISseParser> GetAllParsers()
    {
        return _parsers;
    }
}