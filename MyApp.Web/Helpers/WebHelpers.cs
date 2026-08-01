using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyApp.Web.Helpers;

public static class CookieHelper
{
    public static void SetCookie(HttpContext context, string key, string value, int expireDays = 30)
    {
        var options = new CookieOptions
        {
            Expires = DateTime.UtcNow.AddDays(expireDays),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        };
        context.Response.Cookies.Append(key, value, options);
    }
}

public static class SessionHelper
{
    public static void SetObjectAsJson<T>(ISession session, string key, T value)
    {
        session.SetString(key, System.Text.Json.JsonSerializer.Serialize(value));
    }

    public static T? GetObjectFromJson<T>(ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(value);
    }
}

public static class HtmlHelperExtensions
{
    public static string IsActive(this IHtmlHelper html, string controller, string? action = null)
    {
        var routeData = html.ViewContext.RouteData;
        var routeAction = (string?)routeData.Values["action"];
        var routeController = (string?)routeData.Values["controller"];

        var returnActive = string.Equals(controller, routeController, StringComparison.OrdinalIgnoreCase) &&
                           (action == null || string.Equals(action, routeAction, StringComparison.OrdinalIgnoreCase));

        return returnActive ? "active" : "";
    }
}
