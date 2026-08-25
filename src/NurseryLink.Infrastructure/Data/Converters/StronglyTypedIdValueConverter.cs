using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NurseryLink.Infrastructure.Data.Converters;

/// <summary>
/// Stores a strongly-typed id as a plain <c>uniqueidentifier</c> column, so the typing costs
/// nothing at the database level. Registered once per id type in
/// <c>ApplicationDbContext.ConfigureConventions</c>, which also covers the nullable form.
/// </summary>
public sealed class StronglyTypedIdValueConverter<TId> : ValueConverter<TId, Guid>
    where TId : struct, IStronglyTypedId<TId>
{
    // Held as delegates because an expression tree may not contain an access to a static abstract
    // interface member (CS8927). Building the delegates outside the tree and invoking them inside
    // keeps the converter generic without a hand-written class per id type.
    private static readonly Func<TId, Guid> Unwrap = id => id.Value;
    private static readonly Func<Guid, TId> Create = TId.From;

    public StronglyTypedIdValueConverter()
        : base(id => Unwrap(id), value => Create(value))
    {
    }
}
