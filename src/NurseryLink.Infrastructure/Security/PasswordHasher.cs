using Microsoft.AspNetCore.Identity;
using NurseryLink.Application.Common.Interfaces;
using IdentityHasher = Microsoft.AspNetCore.Identity.PasswordHasher<object>;

namespace NurseryLink.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    // The default implementation ignores the user argument entirely, so a placeholder is safe here.
    private static readonly object HashUser = new();

    private readonly IdentityHasher _hasher = new();

    public string Hash(string password) =>
        _hasher.HashPassword(HashUser, password);

    /// <summary>
    /// Returns the full outcome rather than a bool so the caller can act on
    /// <see cref="PasswordVerificationOutcome.SuccessRehashNeeded"/>. Without that, stored hashes
    /// stay on whatever parameters they were created with even after ASP.NET Core raises its
    /// defaults.
    /// </summary>
    public PasswordVerificationOutcome Verify(string hashedPassword, string providedPassword) =>
        _hasher.VerifyHashedPassword(HashUser, hashedPassword, providedPassword) switch
        {
            PasswordVerificationResult.Success => PasswordVerificationOutcome.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationOutcome.SuccessRehashNeeded,
            _ => PasswordVerificationOutcome.Failed
        };
}
