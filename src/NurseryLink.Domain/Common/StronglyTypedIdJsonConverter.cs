using System.Text.Json;
using System.Text.Json.Serialization;

namespace NurseryLink.Domain.Common;

/// <summary>
/// Serialises a strongly-typed id as a bare GUID string so the wire format is identical to what
/// clients saw before the ids became typed (<c>"id": "7f9c..."</c>, not <c>"id": { "value": ... }</c>).
/// </summary>
public sealed class StronglyTypedIdJsonConverter<TId> : JsonConverter<TId>
    where TId : struct, IStronglyTypedId<TId>
{
    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        TId.From(reader.GetGuid());

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);

    public override TId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        TId.From(Guid.Parse(reader.GetString()!));

    public override void WriteAsPropertyName(Utf8JsonWriter writer, TId value, JsonSerializerOptions options) =>
        writer.WritePropertyName(value.Value.ToString());
}
