using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

// ---------------------------------------------------------------------------
// Fair, isolated comparison: BOTH paths build an identical JSON string using
// identical string-building/escaping logic. The ONLY thing that differs is
// how each property's value is read off the object:
//   - "Naive"  calls Type.GetProperties() and PropertyInfo.GetValue() fresh
//              on every single call.
//   - "Cached" uses a cached PropertyInfo[] plus a compiled Expression-tree
//              delegate (same strategy as the library's ReflectionCache).
// ---------------------------------------------------------------------------

var user = new BenchUser { Id = 1, Name = "John", IsActive = true };
const int warmupIterations = 50_000;
const int measuredIterations = 500_000;
const int trials = 5;

string NaiveSerialize(BenchUser u)
{
    var props = u.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var sb = new StringBuilder(64);
    sb.Append('{');
    for (int p = 0; p < props.Length; p++)
    {
        if (p > 0) sb.Append(',');
        sb.Append('"').Append(props[p].Name).Append("\":");
        AppendJsonValue(sb, props[p].GetValue(u));
    }
    sb.Append('}');
    return sb.ToString();
}

string CachedSerialize(BenchUser u)
{
    var props = ReflectionCacheAccessor.GetProperties(u.GetType());
    var sb = new StringBuilder(64);
    sb.Append('{');
    for (int p = 0; p < props.Length; p++)
    {
        if (p > 0) sb.Append(',');
        sb.Append('"').Append(props[p].Property.Name).Append("\":");
        AppendJsonValue(sb, props[p].Getter(u));
    }
    sb.Append('}');
    return sb.ToString();
}

void AppendJsonValue(StringBuilder sb, object? value)
{
    switch (value)
    {
        case null: sb.Append("null"); break;
        case string s: sb.Append('"').Append(s).Append('"'); break;
        case bool b: sb.Append(b ? "true" : "false"); break;
        default: sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)); break;
    }
}

double RunTrial(Func<BenchUser, string> serialize, int iterations)
{
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
        _ = serialize(user);
    sw.Stop();
    return sw.Elapsed.TotalMilliseconds;
}

// Warm up BOTH paths before measuring either one.
RunTrial(NaiveSerialize, warmupIterations);
RunTrial(CachedSerialize, warmupIterations);

var naiveTimes = new List<double>();
var cachedTimes = new List<double>();

// Alternate order each trial to cancel out ordering bias.
for (int t = 0; t < trials; t++)
{
    if (t % 2 == 0)
    {
        naiveTimes.Add(RunTrial(NaiveSerialize, measuredIterations));
        cachedTimes.Add(RunTrial(CachedSerialize, measuredIterations));
    }
    else
    {
        cachedTimes.Add(RunTrial(CachedSerialize, measuredIterations));
        naiveTimes.Add(RunTrial(NaiveSerialize, measuredIterations));
    }
}

double naiveAvg = naiveTimes.Average();
double cachedAvg = cachedTimes.Average();

Console.WriteLine($"Naive reflection (GetProperties+GetValue every call): {naiveAvg,8:F1} ms avg over {trials} trials  [{string.Join(", ", naiveTimes.Select(x => x.ToString("F1")))}]");
Console.WriteLine($"Cached (compiled delegates):                          {cachedAvg,8:F1} ms avg over {trials} trials  [{string.Join(", ", cachedTimes.Select(x => x.ToString("F1")))}]");
Console.WriteLine($"Speedup: {naiveAvg / cachedAvg:F2}x");

public class BenchUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
}

internal sealed class BenchPropertyAccessor
{
    public PropertyInfo Property { get; }
    public Func<object, object?> Getter { get; }

    public BenchPropertyAccessor(PropertyInfo prop)
    {
        Property = prop;
        var target = System.Linq.Expressions.Expression.Parameter(typeof(object), "t");
        var cast = System.Linq.Expressions.Expression.Convert(target, prop.DeclaringType!);
        var access = System.Linq.Expressions.Expression.Property(cast, prop);
        var boxed = System.Linq.Expressions.Expression.Convert(access, typeof(object));
        Getter = System.Linq.Expressions.Expression.Lambda<Func<object, object?>>(boxed, target).Compile();
    }
}

internal static class ReflectionCacheAccessor
{
    private static readonly Dictionary<Type, BenchPropertyAccessor[]> Cache = new();

    public static BenchPropertyAccessor[] GetProperties(Type type)
    {
        if (Cache.TryGetValue(type, out var cached)) return cached;

        var props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Select(p => new BenchPropertyAccessor(p))
            .ToArray();

        Cache[type] = props;
        return props;
    }
}