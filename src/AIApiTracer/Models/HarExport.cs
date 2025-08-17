using System.Text.Json.Serialization;

namespace AIApiTracer.Models;

public class HarLog
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.2";
    
    [JsonPropertyName("creator")]
    public HarCreator Creator { get; set; } = new();
    
    [JsonPropertyName("entries")]
    public List<HarEntry> Entries { get; set; } = new();
}

public class HarCreator
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "AIApiTracer";
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}

public class HarEntry
{
    [JsonPropertyName("startedDateTime")]
    public string StartedDateTime { get; set; } = string.Empty;
    
    [JsonPropertyName("time")]
    public long Time { get; set; }
    
    [JsonPropertyName("request")]
    public HarRequest Request { get; set; } = new();
    
    [JsonPropertyName("response")]
    public HarResponse Response { get; set; } = new();
    
    [JsonPropertyName("timings")]
    public HarTimings Timings { get; set; } = new();
}

public class HarRequest
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("httpVersion")]
    public string HttpVersion { get; set; } = "HTTP/1.1";
    
    [JsonPropertyName("headers")]
    public List<HarHeader> Headers { get; set; } = new();
    
    [JsonPropertyName("queryString")]
    public List<HarQueryString> QueryString { get; set; } = new();
    
    [JsonPropertyName("postData")]
    public HarPostData? PostData { get; set; }
    
    [JsonPropertyName("headersSize")]
    public long HeadersSize { get; set; } = -1;
    
    [JsonPropertyName("bodySize")]
    public long BodySize { get; set; } = -1;
}

public class HarResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }
    
    [JsonPropertyName("statusText")]
    public string StatusText { get; set; } = string.Empty;
    
    [JsonPropertyName("httpVersion")]
    public string HttpVersion { get; set; } = "HTTP/1.1";
    
    [JsonPropertyName("headers")]
    public List<HarHeader> Headers { get; set; } = new();
    
    [JsonPropertyName("content")]
    public HarContent Content { get; set; } = new();
    
    [JsonPropertyName("redirectURL")]
    public string RedirectURL { get; set; } = string.Empty;
    
    [JsonPropertyName("headersSize")]
    public long HeadersSize { get; set; } = -1;
    
    [JsonPropertyName("bodySize")]
    public long BodySize { get; set; } = -1;
}

public class HarHeader
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class HarQueryString
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class HarPostData
{
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class HarContent
{
    [JsonPropertyName("size")]
    public long Size { get; set; }
    
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("encoding")]
    public string? Encoding { get; set; }
}

public class HarTimings
{
    [JsonPropertyName("send")]
    public long Send { get; set; } = -1;
    
    [JsonPropertyName("wait")]
    public long Wait { get; set; } = -1;
    
    [JsonPropertyName("receive")]
    public long Receive { get; set; } = -1;
}