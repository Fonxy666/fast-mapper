using System.Linq.Expressions;

namespace FastMapper.Model;

public sealed class MapperConfig<TSource, TDestination> : IMapperConfig
{
    private readonly List<string> _ignoredProps = [];

    public MapperConfig<TSource, TDestination> Ignore<TProp>(Expression<Func<TDestination, TProp>> destProp)
    {
        var generatedProp = string.Join(".", destProp.Body.ToString().Split(".").Skip(1));
        _ignoredProps.Add(generatedProp);

        return this;
    }

    public bool IsIgnored(string prop) => _ignoredProps.Any(p => p == prop);
}