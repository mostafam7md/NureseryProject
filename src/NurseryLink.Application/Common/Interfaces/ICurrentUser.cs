using NurseryLink.Domain.Enums;

namespace NurseryLink.Application.Common.Interfaces;

public interface ICurrentUser
{
    AccountId? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin { get; }

    bool IsTeacher { get; }

    bool IsParent { get; }

    bool HasPrivilege(Privilege privilege);

    string? IpAddress { get; }
}
