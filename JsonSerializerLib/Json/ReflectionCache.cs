using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace JsonSerializerLib;

internal sealed class PropertyAccessor
{
    public PropertyInfo Property { get; }
    public Func<object, object?> Getter { get; }
    public Action<object, object?> Setter { get; }

    public PropertyAccessor(PropertyInfo prop)
    {
        Property = prop;
        Getter = BuildGetter(prop);
        Setter = prop.CanWrite ? BuildSetter(prop) : (_, _) => { };
    }

    private static Func<object, object?> BuildGetter(PropertyInfo prop)
    {
        var target = Expression.Parameter(typeof(object), "t");
        var cast = Expression.Convert(target, prop.DeclaringType!);
        var access = Expression.Property(cast, prop);
        var boxed = Expression.Convert(access, typeof(object));
        return Expression.Lambda<Func<object, object?>>(boxed, target).Compile();
    }

    private static Action<object, object?> BuildSetter(PropertyInfo prop)
    {
        var target = Expression.Parameter(typeof(object), "t");
        var value = Expression.Parameter(typeof(object), "v");
        var castTarget = Expression.Convert(target, prop.DeclaringType!);
        var castValue = Expression.Convert(value, prop.PropertyType);
        var call = Expression.Call(castTarget, prop.GetSetMethod(true)!, castValue);
        return Expression.Lambda<Action<object, object?>>(call, target, value).Compile();
    }
}

internal static class ReflectionCache
{
    private static readonly ConcurrentDictionary<Type, PropertyAccessor[]> Cache = new();

    public static PropertyAccessor[] GetProperties(Type type) =>
        Cache.GetOrAdd(type, t => t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Select(p => new PropertyAccessor(p))
            .ToArray());
}
