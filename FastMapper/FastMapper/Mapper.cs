using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMapper;

public static class Mapper<TSource, TDestination>
{
    private static readonly PropertyInfo[] DestProps =
        typeof(TDestination)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

    private static readonly Dictionary<string, PropertyInfo> SrcProps =
        typeof(TSource)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => p);

    private static Func<TSource, TDestination>? _defaultMapFunc;

    public static readonly ConcurrentDictionary<IMapperConfig, Func<TSource, TDestination>>
        _configCache = new();

    public static TDestination Map(TSource source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        _defaultMapFunc ??= CreateMapFunc();
        return _defaultMapFunc(source);
    }

    public static TDestination Map(
        TSource source,
        IMapperConfig? config,
        string? parentProp = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (config == null)
            return Map(source);

        if (!_configCache.TryGetValue(config, out var func))
        {
            func = CreateMapFunc(config, parentProp);
            _configCache[config] = func;
        }

        return func(source);
    }


    private static Func<TSource, TDestination> CreateMapFunc(
        IMapperConfig? config = null,
        string? parentProp = null)
    {
        var parameter = Expression.Parameter(typeof(TSource), "src");
        var bindings = new List<MemberBinding>();

        foreach (var destProp in DestProps)
        {
            var currentProp = parentProp == null
                ? destProp.Name
                : parentProp + "." + destProp.Name;

            if (config?.IsIgnored(currentProp) == true)
            {
                bindings.Add(
                    Expression.Bind(
                        destProp,
                        Expression.Default(destProp.PropertyType)
                    )
                );
                continue;
            }

            if (!SrcProps.TryGetValue(destProp.Name, out var srcProp))
                continue;

            var destType = destProp.PropertyType;
            var srcType = srcProp.PropertyType;

            if (destType.IsClass && destType != typeof(string))
            {
                var innerMapperType =
                    typeof(Mapper<,>).MakeGenericType(srcType, destType);

                var mapMethod =
                    innerMapperType
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .Single(m => m.Name == "Map" &&
                                     m.GetParameters().Length == 3);

                var srcAccess = Expression.Property(parameter, srcProp);

                var call = Expression.Condition(
                    Expression.Equal(srcAccess, Expression.Constant(null, srcType)),
                    Expression.Constant(null, destType),
                    Expression.Call(
                        mapMethod,
                        srcAccess,
                        Expression.Constant(config, typeof(IMapperConfig)),
                        Expression.Constant(currentProp, typeof(string))
                    )
                );

                bindings.Add(Expression.Bind(destProp, call));
                continue;
            }

            if (srcType == destType)
            {
                bindings.Add(
                    Expression.Bind(
                        destProp,
                        Expression.Property(parameter, srcProp)
                    )
                );
            }
        }

        var body = Expression.MemberInit(
            Expression.New(typeof(TDestination)),
            bindings
        );

        return Expression
            .Lambda<Func<TSource, TDestination>>(body, parameter)
            .Compile();
    }
}