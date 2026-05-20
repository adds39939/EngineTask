using System.Collections.Generic;

namespace EngineTask.Generator.CustomFlavours;

// Parses an enginetask.json (string body) into CustomFlavourData list.
// On malformed input returns null and sets errorMessage. Source
// generators must never throw across the analyzer boundary — the
// caller (EngineTaskGenerator) converts the message into ENGTASK006.
//
// Schema:
//   {
//     "flavours": [
//       {
//         "id": "Awaitable",
//         "namespaceSuffix": "UnityAwaitable",
//         "typeMappings":   { "<sourceMetadataName>": "<targetText>", ... },
//         "memberMappings": { "<sourceMetadataName>": "<targetText>", ... }
//       }
//     ]
//   }
//
// Unknown extra keys are tolerated. typeMappings/memberMappings may
// be omitted (empty mapping).
public static class CustomFlavourParser
{
    public static IReadOnlyList<CustomFlavourData>? TryParse(string text, out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            var root = MiniJson.Parse(text) as IReadOnlyDictionary<string, object?>;
            if (root is null)
            {
                errorMessage = "Root must be a JSON object";
                return null;
            }

            if (!root.TryGetValue("flavours", out var flavoursNode) || flavoursNode is null)
                return System.Array.Empty<CustomFlavourData>();

            if (flavoursNode is not IReadOnlyList<object?> flavoursArray)
            {
                errorMessage = "'flavours' must be a JSON array";
                return null;
            }

            var result = new List<CustomFlavourData>(flavoursArray.Count);
            for (var i = 0; i < flavoursArray.Count; i++)
            {
                var item = flavoursArray[i];
                if (item is not IReadOnlyDictionary<string, object?> obj)
                {
                    errorMessage = $"flavours[{i}] must be an object";
                    return null;
                }

                if (!TryReadString(obj, "id", out var id, out errorMessage)) return null;
                if (!TryReadString(obj, "namespaceSuffix", out var nsSuffix, out errorMessage)) return null;
                if (!TryReadMappings(obj, "typeMappings", out var typeMappings, out errorMessage)) return null;
                if (!TryReadMappings(obj, "memberMappings", out var memberMappings, out errorMessage)) return null;

                result.Add(new CustomFlavourData(
                    id!,
                    nsSuffix!,
                    new EquatableArray<CustomMapping>(typeMappings),
                    new EquatableArray<CustomMapping>(memberMappings)));
            }
            return result;
        }
        catch (MiniJsonException ex)
        {
            errorMessage = ex.Message;
            return null;
        }
    }

    private static bool TryReadString(
        IReadOnlyDictionary<string, object?> obj,
        string key,
        out string? value,
        out string? errorMessage)
    {
        if (!obj.TryGetValue(key, out var node) || node is not string s || string.IsNullOrEmpty(s))
        {
            value = null;
            errorMessage = $"Missing or empty '{key}'";
            return false;
        }
        value = s;
        errorMessage = null;
        return true;
    }

    private static bool TryReadMappings(
        IReadOnlyDictionary<string, object?> obj,
        string key,
        out CustomMapping[] mappings,
        out string? errorMessage)
    {
        if (!obj.TryGetValue(key, out var node) || node is null)
        {
            mappings = System.Array.Empty<CustomMapping>();
            errorMessage = null;
            return true;
        }

        if (node is not IReadOnlyDictionary<string, object?> dict)
        {
            mappings = System.Array.Empty<CustomMapping>();
            errorMessage = $"'{key}' must be a JSON object";
            return false;
        }

        var arr = new CustomMapping[dict.Count];
        var i = 0;
        foreach (var pair in dict)
        {
            if (pair.Value is not string to)
            {
                mappings = System.Array.Empty<CustomMapping>();
                errorMessage = $"Mapping value for '{key}.{pair.Key}' must be a string";
                return false;
            }
            arr[i++] = new CustomMapping(pair.Key, to);
        }
        mappings = arr;
        errorMessage = null;
        return true;
    }
}
