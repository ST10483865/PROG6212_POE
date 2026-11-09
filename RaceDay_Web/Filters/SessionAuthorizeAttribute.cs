using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay_Web.Services;

namespace RaceDay_Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string? Roles { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.Session.GetCurrentUser();

        if (user is null)
        {
            var request = context.HttpContext.Request;
            var returnUrl = request.Path.Value + request.QueryString.Value;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (!string.IsNullOrWhiteSpace(Roles))
        {
            var allowed = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Contains(user.Role, StringComparer.OrdinalIgnoreCase))
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
