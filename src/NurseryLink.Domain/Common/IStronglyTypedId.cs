namespace NurseryLink.Domain.Common;

/// <summary>
/// Contract every strongly-typed identifier implements. The static abstract members let generic
/// infrastructure (EF value converters, JSON converters, model binders) construct an id without
/// reflection or a per-type converter class.
/// </summary>
public interface IStronglyTypedId<TSelf>
    where TSelf : struct, IStronglyTypedId<TSelf>
{
    Guid Value { get; }

    /// <summary>Wraps an existing <see cref="Guid"/>.</summary>
    static abstract TSelf From(Guid value);

    /// <summary>
    /// Creates a new identifier. Uses a version-7 (time-ordered) GUID so clustered primary keys
    /// stay sequential and do not fragment the index the way <see cref="Guid.NewGuid"/> does.
    /// </summary>
    static abstract TSelf New();
}
