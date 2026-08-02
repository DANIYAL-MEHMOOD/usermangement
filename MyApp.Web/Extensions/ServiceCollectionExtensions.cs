using Microsoft.AspNetCore.Authentication.Cookies;
using MyApp.Web.ApiClient;
using MyApp.Web.Filters;
using MyApp.Web.Services.Implementations;
using MyApp.Web.Services.Interfaces;

namespace MyApp.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration)
    {
        var apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:7001/";

        services.AddHttpClient<IApiClient, ApiClient.ApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        });

        services.AddScoped<AdminOnlyFilter>();
        services.AddScoped<AuthGuardFilter>();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditService, AuditService>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Error/AccessDenied";
                options.Cookie.Name = ".MyApp.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
            });

        return services;
    }
}
