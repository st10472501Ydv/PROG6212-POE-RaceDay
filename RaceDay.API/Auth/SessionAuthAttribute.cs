using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.API.Auth;

// [SessionAuth]              -> any logged-in user
// [SessionAuth("Organiser")] -> that role only
// 401 when there is no session, 403 when the role does not match.
public class SessionAuthAttribute : ActionFilterAttribute
{
    private readonly string? _role;

    public SessionAuthAttribute(string? role = null) => _role = role;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Session.GetInt32("UserId") is null)
        {
            context.Result = new ObjectResult(new { message = "You must be logged in." }) { StatusCode = 401 };
            return;
        }

        if (_role != null && context.HttpContext.Session.GetString("Role") != _role)
        {
            context.Result = new ObjectResult(new { message = $"Only users with the {_role} role can do this." }) { StatusCode = 403 };
        }
    }
}
