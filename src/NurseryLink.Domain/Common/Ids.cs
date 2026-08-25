using System.ComponentModel;
using System.Text.Json.Serialization;

namespace NurseryLink.Domain.Common;

/// <summary>Identifier of any <see cref="Entities.Account"/> (Admin, Teacher or Parent).</summary>
/// <remarks>
/// Accounts use table-per-type inheritance and therefore share one identity space, so a single
/// <c>AccountId</c> covers all three account kinds rather than having AdminId/TeacherId/ParentId.
/// </remarks>
[TypeConverter(typeof(StronglyTypedIdTypeConverter<AccountId>))]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<AccountId>))]
public readonly record struct AccountId(Guid Value) : IStronglyTypedId<AccountId>
{
    public static AccountId From(Guid value) => new(value);
    public static AccountId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<StudentId>))]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<StudentId>))]
public readonly record struct StudentId(Guid Value) : IStronglyTypedId<StudentId>
{
    public static StudentId From(Guid value) => new(value);
    public static StudentId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<ClassId>))]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<ClassId>))]
public readonly record struct ClassId(Guid Value) : IStronglyTypedId<ClassId>
{
    public static ClassId From(Guid value) => new(value);
    public static ClassId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<ClassTeacherId>))]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<ClassTeacherId>))]
public readonly record struct ClassTeacherId(Guid Value) : IStronglyTypedId<ClassTeacherId>
{
    public static ClassTeacherId From(Guid value) => new(value);
    public static ClassTeacherId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<ActivityLogId>))]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<ActivityLogId>))]
public readonly record struct ActivityLogId(Guid Value) : IStronglyTypedId<ActivityLogId>
{
    public static ActivityLogId From(Guid value) => new(value);
    public static ActivityLogId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<NotificationId>))]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<NotificationId>))]
public readonly record struct NotificationId(Guid Value) : IStronglyTypedId<NotificationId>
{
    public static NotificationId From(Guid value) => new(value);
    public static NotificationId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<AuditLogId>))]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<AuditLogId>))]
public readonly record struct AuditLogId(Guid Value) : IStronglyTypedId<AuditLogId>
{
    public static AuditLogId From(Guid value) => new(value);
    public static AuditLogId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<RefreshTokenId>))]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<RefreshTokenId>))]
public readonly record struct RefreshTokenId(Guid Value) : IStronglyTypedId<RefreshTokenId>
{
    public static RefreshTokenId From(Guid value) => new(value);
    public static RefreshTokenId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
