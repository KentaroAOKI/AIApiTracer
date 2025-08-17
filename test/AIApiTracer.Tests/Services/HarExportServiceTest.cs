using AIApiTracer.Models;
using AIApiTracer.Services;
using System.Text.Json;

namespace AIApiTracer.Tests.Services;

public class HarExportServiceTest
{
    private readonly HarExportService _service = new();

    [Fact]
    public void ExportToHar_WithEmptyTraces_ReturnsEmptyHarLog()
    {
        var traces = new List<ApiTraceRecord>();
        var result = _service.ExportToHar(traces);

        Assert.NotNull(result);
        Assert.Equal("1.2", result.Version);
        Assert.Equal("AIApiTracer", result.Creator.Name);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public void ExportToHar_WithSingleTrace_ReturnsHarLogWithOneEntry()
    {
        var trace = new ApiTraceRecord
        {
            Id = Guid.NewGuid(),
            Timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Method = "POST",
            Url = "/endpoint/openai/v1/chat/completions",
            TargetUrl = "https://api.openai.com/v1/chat/completions",
            StatusCode = 200,
            Duration = 1500,
            Request = new RequestData
            {
                Headers = new Dictionary<string, string[]>
                {
                    { "Content-Type", new[] { "application/json" } },
                    { "Authorization", new[] { "Bearer sk-***" } }
                },
                Body = """{"model":"gpt-4","messages":[{"role":"user","content":"Hello"}]}"""
            },
            Response = new ResponseData
            {
                Headers = new Dictionary<string, string[]>
                {
                    { "Content-Type", new[] { "application/json" } }
                },
                Body = """{"choices":[{"message":{"role":"assistant","content":"Hi there!"}}]}"""
            }
        };

        var traces = new List<ApiTraceRecord> { trace };
        var result = _service.ExportToHar(traces);

        Assert.NotNull(result);
        Assert.Single(result.Entries);

        var entry = result.Entries.First();
        Assert.Equal("2024-01-01T12:00:00.000Z", entry.StartedDateTime);
        Assert.Equal(1500, entry.Time);
        Assert.Equal("POST", entry.Request.Method);
        Assert.Equal("https://api.openai.com/v1/chat/completions", entry.Request.Url);
        Assert.Equal(200, entry.Response.Status);
        Assert.Equal("OK", entry.Response.StatusText);
    }

    [Fact]
    public void ExportToHarJson_WithValidTrace_ReturnsValidJson()
    {
        var trace = new ApiTraceRecord
        {
            Method = "GET",
            Url = "/test",
            TargetUrl = "https://example.com/test",
            StatusCode = 200,
            Duration = 100
        };

        var traces = new List<ApiTraceRecord> { trace };
        var result = _service.ExportToHarJson(traces);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        var harRoot = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.True(harRoot.TryGetProperty("log", out var logElement));
        Assert.True(logElement.TryGetProperty("version", out var versionElement));
        Assert.Equal("1.2", versionElement.GetString());
    }

    [Fact]
    public void ConvertToHarRequest_WithPostData_IncludesPostData()
    {
        var trace = new ApiTraceRecord
        {
            Method = "POST",
            Url = "https://example.com/api",
            Request = new RequestData
            {
                Headers = new Dictionary<string, string[]>
                {
                    { "Content-Type", new[] { "application/json" } }
                },
                Body = """{"key":"value"}"""
            }
        };

        var result = _service.ExportToHar(new[] { trace });
        var request = result.Entries.First().Request;

        Assert.NotNull(request.PostData);
        Assert.Equal("application/json", request.PostData.MimeType);
        Assert.Equal("""{"key":"value"}""", request.PostData.Text);
        Assert.Equal(15, request.BodySize); // Length of {"key":"value"}
    }

    [Fact]
    public void ConvertToHarRequest_WithQueryString_ExtractsQueryParameters()
    {
        var trace = new ApiTraceRecord
        {
            Method = "GET",
            TargetUrl = "https://example.com/api?param1=value1&param2=value2"
        };

        var result = _service.ExportToHar(new[] { trace });
        var request = result.Entries.First().Request;

        Assert.Equal(2, request.QueryString.Count);
        Assert.Contains(request.QueryString, q => q.Name == "param1" && q.Value == "value1");
        Assert.Contains(request.QueryString, q => q.Name == "param2" && q.Value == "value2");
    }

    [Fact]
    public void ConvertToHarResponse_WithHeaders_ConvertsHeaders()
    {
        var trace = new ApiTraceRecord
        {
            Response = new ResponseData
            {
                Headers = new Dictionary<string, string[]>
                {
                    { "Content-Type", new[] { "application/json" } },
                    { "Cache-Control", new[] { "no-cache", "no-store" } }
                },
                Body = "test response"
            },
            StatusCode = 201
        };

        var result = _service.ExportToHar(new[] { trace });
        var response = result.Entries.First().Response;

        Assert.Equal(201, response.Status);
        Assert.Equal("Created", response.StatusText);
        Assert.Equal(3, response.Headers.Count); // Content-Type + 2 Cache-Control headers
        Assert.Contains(response.Headers, h => h.Name == "Content-Type" && h.Value == "application/json");
        Assert.Contains(response.Headers, h => h.Name == "Cache-Control" && h.Value == "no-cache");
        Assert.Contains(response.Headers, h => h.Name == "Cache-Control" && h.Value == "no-store");
    }
}