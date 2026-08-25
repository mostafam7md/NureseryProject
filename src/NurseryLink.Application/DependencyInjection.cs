using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NurseryLink.Application.Features.Admins;
using NurseryLink.Application.Features.Auth;

namespace NurseryLink.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
