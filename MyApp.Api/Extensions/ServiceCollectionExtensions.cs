using MyApp.Api.Data;
using MyApp.Api.Security;
using MyApp.Api.Services.Implementations;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Data access: single centralized ADO.NET helper, no repository layer.
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<SqlDataAccess>();

        // Security
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ISessionTokenService, SessionTokenService>();

        // Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
