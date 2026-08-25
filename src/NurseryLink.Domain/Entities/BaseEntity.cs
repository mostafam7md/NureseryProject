using NurseryLink.Domain.Common;

namespace NurseryLink.Domain.Entities;

public abstract class BaseEntity<TId>
    where TId : struct, IStronglyTypedId<TId>
{
    public TId Id { get; set; } = TId.New();

    /// <summary>When the row was created. Not the same as a domain timestamp such as
    /// <see cref="ActivityLog.LoggedAtUtc"/>, which may be back-dated by the caller.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
