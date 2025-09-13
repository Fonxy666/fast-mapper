using System.Linq.Expressions;

namespace FastMapper;

public class MapperConfig<TSource, TDestination>
{
    private readonly HashSet<string> _ignoredProperties = new();

    public MapperConfig<TSource, TDestination> Ignore<TProp>(Expression<Func<TDestination, TProp>> destProp)
    {
        if (destProp.Body is MemberExpression member)
        {
            _ignoredProperties.Add(member.Member.Name);
        }
        return this;
    }

    internal bool IsIgnored(string propName) => _ignoredProperties.Contains(propName);
}
