using System.Text.Json;

namespace AIApiTracer.Services.MessageParsing;

/// <summary>
/// Base class for message parsers
/// </summary>
public abstract class BaseMessageParser : IMessageParser
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public abstract ParsedMessageData Parse(string json, bool isRequest);
    public abstract bool CanParse(EndpointType endpointType);

    /// <summary>
    /// Safely deserializes JSON
    /// </summary>
    protected T? TryDeserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a JSON string to JsonDocument
    /// </summary>
    protected JsonDocument? TryParseJson(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts other data from JSON that wasn't captured in specific fields
    /// </summary>
    protected Dictionary<string, JsonElement> ExtractOtherData(JsonElement root, HashSet<string> knownFields)
    {
        var otherData = new Dictionary<string, JsonElement>();

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (!knownFields.Contains(property.Name))
                {
                    otherData[property.Name] = property.Value.Clone();
                }
            }
        }

        return otherData;
    }
}