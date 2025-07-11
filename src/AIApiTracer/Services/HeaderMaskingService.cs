using System.Text.RegularExpressions;

namespace AIApiTracer.Services;

public interface IHeaderMaskingService
{
    Dictionary<string, string[]> MaskSensitiveHeaders(Dictionary<string, string[]> headers);
    string MaskValue(string headerName, string value);
}

public partial class HeaderMaskingService : IHeaderMaskingService
{
    private readonly HashSet<string> _sensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization",
        "api-key",
        "x-api-key",
        "apikey",
        "x-apikey",
        "api_key",
        "x_api_key",
        "openai-api-key",
        "anthropic-api-key",
        "x-api-version", // Sometimes contains sensitive info
        "proxy-authorization",
        "x-auth-token",
        "x-access-token",
        "x-secret-key",
        "x-client-secret"
    };

    // Regex patterns for different API key formats
    [GeneratedRegex(@"^Bearer\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"^(sk-[a-zA-Z0-9\-_]+)(.*)$")]
    private static partial Regex OpenAIKeyRegex();

    [GeneratedRegex(@"^(sk-ant-[a-zA-Z0-9\-_]+)(.*)$")]
    private static partial Regex AnthropicKeyRegex();

    public Dictionary<string, string[]> MaskSensitiveHeaders(Dictionary<string, string[]> headers)
    {
        var maskedHeaders = new Dictionary<string, string[]>(headers.Count);

        foreach (var (key, values) in headers)
        {
            if (_sensitiveHeaders.Contains(key))
            {
                var maskedValues = values.Select(v => MaskValue(key, v ?? string.Empty)).ToArray();
                maskedHeaders[key] = maskedValues;
            }
            else
            {
                maskedHeaders[key] = values;
            }
        }

        return maskedHeaders;
    }

    public string MaskValue(string headerName, string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Handle Bearer tokens
        var bearerMatch = BearerTokenRegex().Match(value);
        if (bearerMatch.Success)
        {
            var token = bearerMatch.Groups[1].Value;
            return $"Bearer {MaskApiKey(token)}";
        }

        // Direct API key
        return MaskApiKey(value);
    }

    private string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return apiKey;

        // OpenAI format (sk-...)
        var openAIMatch = OpenAIKeyRegex().Match(apiKey);
        if (openAIMatch.Success)
        {
            var fullKey = openAIMatch.Groups[1].Value;
            return MaskWithAsterisks(fullKey, 6, 3); // Show first 6 and last 3 chars
        }

        // Anthropic format (sk-ant-...)
        var anthropicMatch = AnthropicKeyRegex().Match(apiKey);
        if (anthropicMatch.Success)
        {
            var fullKey = anthropicMatch.Groups[1].Value;
            return MaskWithAsterisks(fullKey, 10, 3); // Show first 10 and last 3 chars
        }

        // Generic masking for other formats
        return MaskWithAsterisks(apiKey, 3, 3); // Show first 3 and last 3 chars
    }

    private string MaskWithAsterisks(string value, int showPrefix, int showSuffix)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var totalShow = showPrefix + showSuffix;
        
        // If the value is too short, mask everything
        if (value.Length <= totalShow)
        {
            return new string('*', value.Length);
        }

        var prefix = value.Substring(0, showPrefix);
        var suffix = GetLastChars(value, showSuffix);
        var maskedLength = value.Length - totalShow;
        var masked = new string('*', maskedLength);

        return $"{prefix}{masked}{suffix}";
    }

    private static string GetLastChars(string value, int count)
    {
        if (value.Length <= count)
            return value;
        return value.Substring(value.Length - count);
    }
}