using System.Collections;
using System.Globalization;

namespace JsonSerializerLib;

internal static class JsonBinder
{
    public static object? Bind(object? node, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying != null)
            return node is null ? null : Bind(node, underlying);

        if (node is null)
        {
            if (targetType.IsValueType)
                throw new JsonDeserializationException(
                    $"Cannot assign JSON null to non-nullable value type '{targetType.Name}'.");
            return null;
        }

        if (targetType == typeof(object)) return CoerceToObject(node);
        if (targetType == typeof(string))
            return node is string s ? s : throw Mismatch(targetType, node);
        if (targetType == typeof(bool))
            return node is bool b ? b : throw Mismatch(targetType, node);

        if (targetType.IsEnum)
        {
            if (node is string es)
            {
                try { return Enum.Parse(targetType, es, ignoreCase: true); }
                catch (Exception ex) { throw new JsonDeserializationException($"'{es}' is not a valid value for enum '{targetType.Name}'.", ex); }
            }
            if (node is JsonNumber jn) return Enum.ToObject(targetType, jn.ToInt());
            throw Mismatch(targetType, node);
        }

        if (targetType == typeof(Guid))
        {
            if (node is string gs && Guid.TryParse(gs, out var g)) return g;
            throw new JsonDeserializationException($"Expected a GUID string but found {Describe(node)}.");
        }

        if (targetType == typeof(DateTime))
        {
            if (node is string ds && DateTime.TryParse(ds, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return dt;
            throw new JsonDeserializationException($"Expected an ISO-8601 date string but found {Describe(node)}.");
        }

        if (targetType == typeof(DateTimeOffset))
        {
            if (node is string dos && DateTimeOffset.TryParse(dos, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                return dto;
            throw new JsonDeserializationException($"Expected an ISO-8601 date string but found {Describe(node)}.");
        }

        if (IsNumeric(targetType))
            return node is JsonNumber num ? ConvertNumber(num, targetType) : throw Mismatch(targetType, node);

        if (typeof(IDictionary).IsAssignableFrom(targetType)) return BindDictionary(node, targetType);
        if (targetType.IsArray) return BindArray(node, targetType);
        if (typeof(IEnumerable).IsAssignableFrom(targetType) && targetType != typeof(string)) return BindCollection(node, targetType);

        return BindObject(node, targetType);
    }

    private static object CoerceToObject(object node) => node switch
    {
        JsonNumber n => n.ToDefaultClrType(),
        Dictionary<string, object?> d => d.ToDictionary(kv => kv.Key, kv => kv.Value is null ? null : CoerceToObject(kv.Value)),
        List<object?> l => l.Select(x => x is null ? null : CoerceToObject(x)).ToList(),
        _ => node
    };

    private static bool IsNumeric(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) ||
        t == typeof(sbyte) || t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) ||
        t == typeof(float) || t == typeof(double) || t == typeof(decimal);

    private static object ConvertNumber(JsonNumber n, Type t)
    {
        try
        {
            if (t == typeof(int)) return n.ToInt();
            if (t == typeof(long)) return n.ToLong();
            if (t == typeof(short)) return (short)n.ToLong();
            if (t == typeof(byte)) return (byte)n.ToLong();
            if (t == typeof(sbyte)) return (sbyte)n.ToLong();
            if (t == typeof(uint)) return (uint)n.ToLong();
            if (t == typeof(ulong)) return (ulong)n.ToDecimal();
            if (t == typeof(ushort)) return (ushort)n.ToLong();
            if (t == typeof(float)) return n.ToFloat();
            if (t == typeof(double)) return n.ToDouble();
            if (t == typeof(decimal)) return n.ToDecimal();
        }
        catch (Exception ex)
        {
            throw new JsonDeserializationException($"Value '{n.Raw}' is out of range or invalid for type '{t.Name}'.", ex);
        }
        throw new JsonDeserializationException($"Unsupported numeric type '{t.Name}'.");
    }

    private static object BindDictionary(object node, Type targetType)
    {
        if (node is not Dictionary<string, object?> src) throw Mismatch(targetType, node);

        var genericArgs = targetType.IsGenericType ? targetType.GetGenericArguments() : Array.Empty<Type>();
        Type keyType = genericArgs.Length == 2 ? genericArgs[0] : typeof(string);
        Type valueType = genericArgs.Length == 2 ? genericArgs[1] : typeof(object);

        IDictionary result;
        if (targetType.IsInterface || targetType.IsAbstract)
        {
            var concrete = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            result = (IDictionary)Activator.CreateInstance(concrete)!;
        }
        else
        {
            try { result = (IDictionary)Activator.CreateInstance(targetType)!; }
            catch (Exception ex) { throw new JsonDeserializationException($"Cannot construct dictionary type '{targetType.Name}'.", ex); }
        }

        foreach (var kv in src)
            result.Add(kv.Key, Bind(kv.Value, valueType));

        return result;
    }

    private static object BindArray(object node, Type targetType)
    {
        if (node is not List<object?> src) throw Mismatch(targetType, node);

        var elemType = targetType.GetElementType()!;
        var arr = Array.CreateInstance(elemType, src.Count);
        for (int i = 0; i < src.Count; i++)
            arr.SetValue(Bind(src[i], elemType), i);
        return arr;
    }

    private static object BindCollection(object node, Type targetType)
    {
        if (node is not List<object?> src) throw Mismatch(targetType, node);

        Type elemType = targetType.IsGenericType ? targetType.GetGenericArguments()[0] : typeof(object);
        var listType = typeof(List<>).MakeGenericType(elemType);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in src)
            list.Add(Bind(item, elemType));

        if (targetType.IsInterface || targetType.IsAssignableFrom(listType))
            return list;

        try { return Activator.CreateInstance(targetType, list)!; }
        catch (Exception ex)
        {
            throw new JsonDeserializationException($"Cannot deserialize a JSON array into type '{targetType.Name}'.", ex);
        }
    }

    private static object BindObject(object node, Type targetType)
    {
        if (node is not Dictionary<string, object?> src) throw Mismatch(targetType, node);

        object instance;
        try { instance = Activator.CreateInstance(targetType)!; }
        catch (Exception ex)
        {
            throw new JsonDeserializationException(
                $"Type '{targetType.Name}' must have a public parameterless constructor to be deserialized.", ex);
        }

        var props = ReflectionCache.GetProperties(targetType)
            .Where(p => p.Property.CanWrite)
            .ToDictionary(p => p.Property.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var kv in src)
        {
            if (!props.TryGetValue(kv.Key, out var accessor))
                continue; // unknown JSON field -> ignored

            try
            {
                accessor.Setter(instance, Bind(kv.Value, accessor.Property.PropertyType));
            }
            catch (JsonDeserializationException) { throw; }
            catch (Exception ex)
            {
                throw new JsonDeserializationException($"Failed to set property '{kv.Key}' on type '{targetType.Name}'.", ex);
            }
        }

        return instance;
    }

    private static JsonDeserializationException Mismatch(Type targetType, object node) =>
        new($"Type mismatch: expected JSON compatible with '{targetType.Name}' but found {Describe(node)}.");

    private static string Describe(object? node) => node switch
    {
        null => "null",
        string => "a string",
        bool => "a boolean",
        JsonNumber n => $"a number ({n.Raw})",
        Dictionary<string, object?> => "an object",
        List<object?> => "an array",
        _ => node.GetType().Name
    };
}
