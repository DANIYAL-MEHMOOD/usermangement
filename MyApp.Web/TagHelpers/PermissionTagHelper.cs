using Microsoft.AspNetCore.Razor.TagHelpers;
using MyApp.Web.Services.Interfaces;

namespace MyApp.Web.TagHelpers;

[HtmlTargetElement("permission-guard")]
public class PermissionTagHelper : TagHelper
{
    private readonly IPermissionService _permissionService;

    public PermissionTagHelper(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HtmlAttributeName("module")]
    public string Module { get; set; } = string.Empty;

    [HtmlAttributeName("action")]
    public string Action { get; set; } = string.Empty;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        if (string.IsNullOrEmpty(Module) || string.IsNullOrEmpty(Action))
        {
            return;
        }

        var hasPerm = await _permissionService.HasPermissionAsync(Module, Action);
        if (!hasPerm)
        {
            output.SuppressOutput();
        }
    }
}
