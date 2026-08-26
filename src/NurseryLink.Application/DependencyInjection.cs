using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NurseryLink.Application.Common.Services;
using NurseryLink.Application.Features.Activities;
using NurseryLink.Application.Features.Admins;
using NurseryLink.Application.Features.Auth;
using NurseryLink.Application.Features.Classes;
using NurseryLink.Application.Features.Parents;
using NurseryLink.Application.Features.Students;
using NurseryLink.Application.Features.Teachers;

namespace NurseryLink.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IAdminGuard, AdminGuard>();
        services.AddScoped<ITeacherGuard, TeacherGuard>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IParentService, ParentService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();

        return services;
    }
}
