using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMapper;

public static class Mapper<TSource, TDestination>
{
    private static Func<TSource, TDestination>? _defaultMapFunc;

    public static readonly ConcurrentDictionary<IMapperConfig, Func<TSource, TDestination>> _configCache
        = new();

    public static TDestination Map(TSource source)
    {
        _defaultMapFunc ??= CreateMapFunc();
        return _defaultMapFunc(source);
    }

    public static TDestination Map(TSource source, IMapperConfig? config, string? parentProp = null)
    {
        if (config == null)
        {
            return Map(source);
        }

        if (!_configCache.TryGetValue(config, out var funcCached))
        {
            funcCached = CreateMapFunc(config, parentProp);
            _configCache[config] = funcCached;
        }

        return funcCached(source);
    }

    private static Func<TSource, TDestination> CreateMapFunc(IMapperConfig? config = null, string? parentProp = null)
    {
        var parameter = Expression.Parameter(typeof(TSource), "src");
        var bindings =  new List<MemberBinding>();
        var actualMemberInfo = new List<MemberInfo>();

        foreach (var destProp in typeof(TDestination).GetProperties())
        {
            var currentProp = parentProp is not null ? string.Join(".", [parentProp, destProp.Name]) : destProp.Name;

            if (config is not null && config.IsIgnored(currentProp))
            {
                if (destProp.PropertyType == typeof(string))
                {
                    bindings.Add(Expression.Bind(destProp, Expression.Constant(string.Empty)));
                }

                continue;
            }

            var srcProp = typeof(TSource).GetProperty(destProp.Name);
            var propType = destProp.PropertyType;

            if (propType.IsClass && propType != typeof(string))
            {
                var innerMapperType = typeof(Mapper<,>).MakeGenericType(propType, destProp.PropertyType);

                var mapMethod = innerMapperType.GetMethod("Map", new[] { srcProp.PropertyType, typeof(IMapperConfig), typeof(string) });
                var configConst = Expression.Constant(config?? null, typeof(IMapperConfig));
                var parentPropConst = Expression.Constant(currentProp, typeof(string));

                var srcAccess = Expression.Property(parameter, srcProp);
                var callExpression = Expression.Condition(
                    Expression.Equal(srcAccess, Expression.Constant(null, srcProp.PropertyType)),
                    Expression.Constant(null, destProp.PropertyType),
                    Expression.Call(mapMethod, srcAccess, configConst, parentPropConst)
                );

                bindings.Add(Expression.Bind(destProp, callExpression));
            }

            else if (srcProp != null && srcProp.PropertyType == destProp.PropertyType)
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

public static class MapperLogger
{
    public static Action<string>? Log { get; set; }
}