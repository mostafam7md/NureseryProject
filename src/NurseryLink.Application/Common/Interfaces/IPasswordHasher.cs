namespace NurseryLink.Application.Common.Interfaces;

public enum PasswordVerificationOutcome
{
    Failed = 0,
    Success = 1,

    /// <summary>Password was correct but the stored hash uses outdated parameters. The caller should
    /// rehash and save so hashes are upgraded transparently as ASP.NET Core raises its defaults.</summary>
    SuccessRehashNeeded = 2
}

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationOutcome Verify(string hashedPassword, string providedPassword);
}
