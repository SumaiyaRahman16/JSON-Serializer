namespace JsonSerializerLib;

/// <summary>Thrown when the raw JSON text is malformed (tokenizer/parser stage).</summary>
public class JsonParseException : Exception
{
    public int Position { get; }

    public JsonParseException(string message, int position)
        : base($"{message} (at character {position})")
    {
        Position = position;
    }
}

/// <summary>Thrown when an object graph cannot be turned into JSON text.</summary>
public class JsonSerializationException : Exception
{
    public JsonSerializationException(string message) : base(message) { }
    public JsonSerializationException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Thrown when parsed JSON cannot be bound onto the requested .NET type.</summary>
public class JsonDeserializationException : Exception
{
    public JsonDeserializationException(string message) : base(message) { }
    public JsonDeserializationException(string message, Exception inner) : base(message, inner) { }
}
