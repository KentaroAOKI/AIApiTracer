namespace AIApiTracer.Models;

public class ApiTraceRecord
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long Duration { get; set; } // in milliseconds
    
    public RequestData Request { get; set; } = new();
    public ResponseData Response { get; set; } = new();
    public AiMetadata? AiMetadata { get; set; }
}

public class RequestData
{
    public Dictionary<string, string[]> Headers { get; set; } = new();
    public string Body { get; set; } = string.Empty;
}

public class ResponseData
{
    public Dictionary<string, string[]> Headers { get; set; } = new();
    public string Body { get; set; } = string.Empty;
    public string? RawSseData { get; set; } // Raw SSE data before parsing
}