namespace NurseryLink.Api;

public static class RateLimitPolicies
{
    /// <summary>Throttles the anonymous credential-accepting endpoints (login, refresh) per client IP.</summary>
    public const string Login = "login";
}
