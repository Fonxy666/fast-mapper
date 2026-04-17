namespace FastMapper.Model;

internal sealed record MapperCacheKey(
    Type Source,
    Type Destination,
    string? ParentPath,
    int ConfigHash
);

