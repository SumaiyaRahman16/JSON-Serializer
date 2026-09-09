namespace JsonSerializerLib;

public static class JsonSerializer
{
    public static string Serialize(object? value, bool indent = true)
    {
        try
        {
            return new JsonWriter(indent).Write(value);
        }
        catch (JsonSerializationException) { throw; }
        catch (Exception ex)
        {
            throw new JsonSerializationException("Failed to serialize object.", ex);
        }
    }

    /// <summary>Deserializes JSON text into an instance of T.</summary>
    public static T? Deserialize<T>(string json)
    {
        var graph = ParseOrThrow(json);
        return (T?)JsonBinder.Bind(graph, typeof(T));
    }

    /// <summary>Deserializes JSON text into an instance of the given type.</summary>
    public static object? Deserialize(string json, Type type)
    {
        var graph = ParseOrThrow(json);
        return JsonBinder.Bind(graph, type);
    }

    private static object? ParseOrThrow(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new JsonParseException("Input JSON was null or empty.", 0);

        return new JsonParser(json).Parse();
    }
}
