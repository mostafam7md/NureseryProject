using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NurseryLink.Application.Common.Services;
using NurseryLink.Application.Features.Admins;
using NurseryLink.Application.Features.Auth;
using NurseryLink.Application.Features.Classes;
using NurseryLink.Application.Features.Teachers;

namespace NurseryLink.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IAdminGuard, AdminGuard>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<IClassService, ClassService>();

        return services;
    }
}
