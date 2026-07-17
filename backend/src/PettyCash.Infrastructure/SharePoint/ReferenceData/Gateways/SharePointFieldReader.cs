using System.Text.Json;

namespace PettyCash.Infrastructure.SharePoint.ReferenceData.Gateways;

internal static class SharePointFieldReader
{
    public static bool TryReadRequiredString(
        IReadOnlyDictionary<string, JsonElement> fields,
        string fieldName,
        out string value)
    {
        value = string.Empty;
        if (!TryGetField(fields, fieldName, out var element))
        {
            return false;
        }

        var candidate = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        value = candidate;
        return true;
    }

    public static string? ReadOptionalString(IReadOnlyDictionary<string, JsonElement> fields, string fieldName)
    {
        return TryGetField(fields, fieldName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    public static bool TryReadBoolean(IReadOnlyDictionary<string, JsonElement> fields, string fieldName, out bool value)
    {
        value = false;
        if (!TryGetField(fields, fieldName, out var element))
        {
            return false;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.True:
                value = true;
                return true;
            case JsonValueKind.False:
                value = false;
                return true;
            case JsonValueKind.Number when element.TryGetInt32(out var intValue):
                value = intValue != 0;
                return true;
            case JsonValueKind.String:
                var text = element.GetString();
                if (bool.TryParse(text, out var boolValue))
                {
                    value = boolValue;
                    return true;
                }

                if (int.TryParse(text, out var stringInt))
                {
                    value = stringInt != 0;
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    public static bool TryGetField(IReadOnlyDictionary<string, JsonElement> fields, string fieldName, out JsonElement value)
    {
        if (fields.TryGetValue(fieldName, out value))
        {
            return true;
        }

        foreach (var pair in fields)
        {
            if (string.Equals(pair.Key, fieldName, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
