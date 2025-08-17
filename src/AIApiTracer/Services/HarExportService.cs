using AIApiTracer.Models;
using System.Text.Json;
using System.Web;

namespace AIApiTracer.Services;

public interface IHarExportService
{
    HarLog ExportToHar(IEnumerable<ApiTraceRecord> traces);
    string ExportToHarJson(IEnumerable<ApiTraceRecord> traces);
}

public class HarExportService : IHarExportService
{
    public HarLog ExportToHar(IEnumerable<ApiTraceRecord> traces)
    {
        var harLog = new HarLog();
        
        foreach (var trace in traces)
        {
            var harEntry = ConvertToHarEntry(trace);
            harLog.Entries.Add(harEntry);
        }
        
        return harLog;
    }

    public string ExportToHarJson(IEnumerable<ApiTraceRecord> traces)
    {
        var harLog = ExportToHar(traces);
        var harRoot = new { log = harLog };
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        return JsonSerializer.Serialize(harRoot, options);
    }

    private static HarEntry ConvertToHarEntry(ApiTraceRecord trace)
    {
        var harEntry = new HarEntry
        {
            StartedDateTime = trace.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Time = trace.Duration,
            Request = ConvertToHarRequest(trace),
            Response = ConvertToHarResponse(trace),
            Timings = new HarTimings
            {
                Send = 0,
                Wait = trace.Duration,
                Receive = 0
            }
        };

        return harEntry;
    }

    private static HarRequest ConvertToHarRequest(ApiTraceRecord trace)
    {
        var harRequest = new HarRequest
        {
            Method = trace.Method,
            Url = trace.TargetUrl.IsNullOrEmpty() ? trace.Url : trace.TargetUrl,
            Headers = ConvertHeaders(trace.Request.Headers),
            QueryString = ExtractQueryString(trace.TargetUrl.IsNullOrEmpty() ? trace.Url : trace.TargetUrl)
        };

        if (!trace.Request.Body.IsNullOrEmpty())
        {
            var contentType = GetContentType(trace.Request.Headers);
            harRequest.PostData = new HarPostData
            {
                MimeType = contentType,
                Text = trace.Request.Body
            };
            harRequest.BodySize = System.Text.Encoding.UTF8.GetByteCount(trace.Request.Body);
        }

        return harRequest;
    }

    private static HarResponse ConvertToHarResponse(ApiTraceRecord trace)
    {
        var harResponse = new HarResponse
        {
            Status = trace.StatusCode,
            StatusText = GetStatusText(trace.StatusCode),
            Headers = ConvertHeaders(trace.Response.Headers),
            Content = new HarContent
            {
                Size = System.Text.Encoding.UTF8.GetByteCount(trace.Response.Body),
                MimeType = GetContentType(trace.Response.Headers),
                Text = trace.Response.Body
            }
        };

        harResponse.BodySize = harResponse.Content.Size;

        return harResponse;
    }

    private static List<HarHeader> ConvertHeaders(Dictionary<string, string[]> headers)
    {
        var harHeaders = new List<HarHeader>();
        
        foreach (var header in headers)
        {
            foreach (var value in header.Value)
            {
                harHeaders.Add(new HarHeader
                {
                    Name = header.Key,
                    Value = value
                });
            }
        }
        
        return harHeaders;
    }

    private static List<HarQueryString> ExtractQueryString(string url)
    {
        var queryString = new List<HarQueryString>();
        
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            foreach (string? key in query.AllKeys)
            {
                if (key != null)
                {
                    var values = query.GetValues(key);
                    if (values != null)
                    {
                        foreach (var value in values)
                        {
                            queryString.Add(new HarQueryString
                            {
                                Name = key,
                                Value = value ?? string.Empty
                            });
                        }
                    }
                }
            }
        }
        
        return queryString;
    }

    private static string GetContentType(Dictionary<string, string[]> headers)
    {
        var contentTypeHeader = headers.FirstOrDefault(h => 
            string.Equals(h.Key, "Content-Type", StringComparison.OrdinalIgnoreCase));
        
        return contentTypeHeader.Value?.FirstOrDefault() ?? "application/octet-stream";
    }

    private static string GetStatusText(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            201 => "Created",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            _ => string.Empty
        };
    }
}

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }
}