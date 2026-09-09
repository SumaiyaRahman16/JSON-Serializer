using System.Collections;
using System.Globalization;
using System.Text;

namespace JsonSerializerLib;

internal sealed class JsonWriter
{
    private readonly StringBuilder _sb = new();
    private readonly bool _indent;
    private int _depth;
    private readonly HashSet<object> _ancestors = new(ReferenceEqualityComparer.Instance);

    public JsonWriter(bool indent) => _indent = indent;

    public string Write(object? value)
    {
        WriteValue(value);
        return _sb.ToString();
    }

    private void NewLineIndent()
    {
        if (!_indent) return;
        _sb.Append('\n').Append(' ', _depth * 2);
    }

    private void WriteValue(object? value)
    {
        switch (value)
        {
            case null: _sb.Append("null"); return;
            case string s: WriteString(s); return;
            case bool b: _sb.Append(b ? "true" : "false"); return;
            case char c: WriteString(c.ToString()); return;
            case Guid g: WriteString(g.ToString()); return;
            case DateTime dt: WriteString(dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)); return;
            case DateTimeOffset dto: WriteString(dto.ToString("o", CultureInfo.InvariantCulture)); return;
            case Enum e: WriteString(e.ToString()); return;
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                _sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); return;
            case float f: _sb.Append(f.ToString("R", CultureInfo.InvariantCulture)); return;
            case double d: _sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); return;
            case decimal dec: _sb.Append(dec.ToString(CultureInfo.InvariantCulture)); return;
            case IDictionary dict: WriteDictionary(dict); return;
            case IEnumerable en: WriteArray(en); return;
            default: WriteObject(value); return;
        }
    }

    private void WriteString(string s)
    {
        _sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': _sb.Append("\\\""); break;
                case '\\': _sb.Append("\\\\"); break;
                case '\b': _sb.Append("\\b"); break;
                case '\f': _sb.Append("\\f"); break;
                case '\n': _sb.Append("\\n"); break;
                case '\r': _sb.Append("\\r"); break;
                case '\t': _sb.Append("\\t"); break;
                default:
                    if (c < 0x20) _sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else _sb.Append(c);
                    break;
            }
        }
        _sb.Append('"');
    }

    private void WriteArray(IEnumerable en)
    {
        if (!TryPush(en)) { _sb.Append("null"); return; }

        _sb.Append('[');
        _depth++;
        bool first = true;
        foreach (var item in en)
        {
            if (!first) _sb.Append(',');
            first = false;
            NewLineIndent();
            WriteValue(item);
        }
        _depth--;
        if (!first) NewLineIndent();
        _sb.Append(']');

        Pop(en);
    }

    private void WriteDictionary(IDictionary dict)
    {
        if (!TryPush(dict)) { _sb.Append("null"); return; }

        _sb.Append('{');
        _depth++;
        bool first = true;
        foreach (DictionaryEntry entry in dict)
        {
            if (!first) _sb.Append(',');
            first = false;
            NewLineIndent();
            WriteString(entry.Key?.ToString() ?? "null");
            _sb.Append(_indent ? ": " : ":");
            WriteValue(entry.Value);
        }
        _depth--;
        if (!first) NewLineIndent();
        _sb.Append('}');

        Pop(dict);
    }

    private void WriteObject(object value)
    {
        if (!TryPush(value)) { _sb.Append("null"); return; }

        var props = ReflectionCache.GetProperties(value.GetType());
        _sb.Append('{');
        _depth++;
        bool first = true;
        foreach (var p in props)
        {
            if (!first) _sb.Append(',');
            first = false;
            NewLineIndent();
            WriteString(p.Property.Name);
            _sb.Append(_indent ? ": " : ":");

            object? v;
            try { v = p.Getter(value); }
            catch (Exception ex)
            {
                throw new JsonSerializationException(
                    $"Failed to read property '{p.Property.Name}' on type '{value.GetType().Name}'.", ex);
            }
            WriteValue(v);
        }
        _depth--;
        if (!first) NewLineIndent();
        _sb.Append('}');

        Pop(value);
    }

    // Only reference types can participate in a cycle.
    private bool TryPush(object value)
    {
        if (value.GetType().IsValueType) return true;
        return _ancestors.Add(value);
    }

    private void Pop(object value)
    {
        if (!value.GetType().IsValueType) _ancestors.Remove(value);
    }
}
