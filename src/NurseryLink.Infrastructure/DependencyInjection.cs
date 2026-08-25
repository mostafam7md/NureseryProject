using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NurseryLink.Application.Common.Interfaces;
using NurseryLink.Infrastructure.Configuration;
using NurseryLink.Infrastructure.Auth;
using NurseryLink.Infrastructure.Data;
using NurseryLink.Infrastructure.Security;

namespace NurseryLink.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // ValidateOnStart turns a missing or too-short signing key into a startup failure with a
        // readable message, instead of an exception on the first login attempt.
        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                s => Encoding.UTF8.GetByteCount(s.SecretKey) >= JwtSettings.MinimumSecretKeyBytes,
                $"{JwtSettings.SectionName}:SecretKey must be at least {JwtSettings.MinimumSecretKeyBytes} bytes for HMAC-SHA256.")
            .ValidateOnStart();

        services.AddOptions<SeedAdminOptions>()
            .BindConfiguration(SeedAdminOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<NurserySettings>()
            .BindConfiguration(NurserySettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseSettings>()
            .BindConfiguration(DatabaseSettings.SectionName);

        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException($"Missing '{JwtSettings.SectionName}' configuration section.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

        // Note: authorization (policies, handlers, fallback policy) is wired in the API layer.
        // Registering it here as well made the custom IAuthorizationPolicyProvider depend on the
        // order the two AddAuthorization calls happened to run in.

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<INurseryClock, NurseryClock>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}
