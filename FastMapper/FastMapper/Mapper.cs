using System.Linq.Expressions;
using System.Collections.Concurrent;

namespace FastMapper;

public static class Mapper<TSource, TDestination>
{
    private static readonly Func<TSource, TDestination> _defaultMapFunc;

    private static readonly ConcurrentDictionary<MapperConfig<TSource, TDestination>, Func<TSource, TDestination>> _configCache
        = new();

    static Mapper()
    {
        _defaultMapFunc = CreateMapFunc(null);
    }

    public static TDestination Map(TSource source) => _defaultMapFunc(source);

    public static TDestination Map(TSource source, MapperConfig<TSource, TDestination> config)
    {
        if (!_configCache.TryGetValue(config, out var func))
        {
            func = CreateMapFunc(config);
            _configCache[config] = func;
        }

        return func(source);
    }

    private static Func<TSource, TDestination> CreateMapFunc(MapperConfig<TSource, TDestination>? config)
    {
        var parameter = Expression.Parameter(typeof(TSource), "src");
        var bindings = new List<MemberBinding>();

        foreach (var destProp in typeof(TDestination).GetProperties())
        {
            if (config != null && config.IsIgnored(destProp.Name)) continue;

            var srcProp = typeof(TSource).GetProperty(destProp.Name);
            if (srcProp != null && srcProp.PropertyType == destProp.PropertyType)
            {
                var propertyAccess = Expression.Property(parameter, srcProp);
                bindings.Add(Expression.Bind(destProp, propertyAccess));
            }
        }

        var body = Expression.MemberInit(Expression.New(typeof(TDestination)), bindings);
        var lambda = Expression.Lambda<Func<TSource, TDestination>>(body, parameter);
        return lambda.Compile();
    }
}