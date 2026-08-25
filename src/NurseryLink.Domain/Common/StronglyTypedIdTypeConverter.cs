using System.ComponentModel;
using System.Globalization;

namespace NurseryLink.Domain.Common;

/// <summary>
/// Lets ASP.NET Core bind a strongly-typed id from a route value or query string. MVC's
/// simple-type model binder looks for a <see cref="TypeConverter"/>, so without this every
/// <c>[FromRoute] StudentId</c> parameter would silently bind to <c>default</c>.
/// </summary>
public sealed class StronglyTypedIdTypeConverter<TId> : TypeConverter
    where TId : struct, IStronglyTypedId<TId>
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || sourceType == typeof(Guid) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || destinationType == typeof(Guid) || base.CanConvertTo(context, destinationType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value switch
        {
            Guid guid => TId.From(guid),
            string text when Guid.TryParse(text, out var parsed) => TId.From(parsed),
            string text => throw new FormatException($"'{text}' is not a valid {typeof(TId).Name}."),
            _ => base.ConvertFrom(context, culture, value)
        };

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is TId id)
        {
            if (destinationType == typeof(string)) return id.Value.ToString();
            if (destinationType == typeof(Guid)) return id.Value;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
