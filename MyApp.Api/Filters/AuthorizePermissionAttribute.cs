using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyApp.Api.Common;
using MyApp.Api.Services.Interfaces;

namespace MyApp.Api.Filters;

/// <summary>
/// Action-level permission authorization: verifies the caller holds a specific
/// permission cell (module + action, e.g. "Users" + "ADD") before the action
/// runs. Runs in addition to role authorization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AuthorizePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _module;
    private readonly string _action;

    public AuthorizePermissionAttribute(string module, string action)
    {
        _module = module;
        _action = action;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        if (currentUser.UserId is not int userId || currentUser.RoleId is not int roleId)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var response = await permissionService.HasPermissionAsync(userId, roleId, _module, _action);

        if (response is not { Success: true, Data: true })
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail(
                "You do not have permission to perform this action.", 403))
            {
                StatusCode = 403
            };
        }
    }
}
