using System.Globalization;

namespace JsonSerializerLib;

internal readonly struct JsonNumber
{
    public string Raw { get; }
    public JsonNumber(string raw) => Raw = raw;

    public double ToDouble() => double.Parse(Raw, CultureInfo.InvariantCulture);
    public float ToFloat() => float.Parse(Raw, CultureInfo.InvariantCulture);
    public decimal ToDecimal() => decimal.Parse(Raw, CultureInfo.InvariantCulture);
    public long ToLong() => long.Parse(Raw, CultureInfo.InvariantCulture);
    public int ToInt() => int.Parse(Raw, CultureInfo.InvariantCulture);

    /// <summary>Used when deserializing into 'object': int if it fits, else long, else double.</summary>
    public object ToDefaultClrType()
    {
        if (Raw.IndexOfAny(new[] { '.', 'e', 'E' }) >= 0) return ToDouble();
        if (long.TryParse(Raw, out long l))
            return l is >= int.MinValue and <= int.MaxValue ? (int)l : l;
        return ToDouble();
    }

    public override string ToString() => Raw;
}
