using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Part2.Data;
using Microsoft.EntityFrameworkCore;

namespace RaceDay.Part2.Authorization;

public class SessionAuthorizationAttribute : Attribute, IAsyncActionFilter
{
    private readonly string? _requiredRole;

    public SessionAuthorizationAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var session = context.HttpContext.Session;

        int? userId = session.GetInt32("UserID");
        string? role = session.GetString("Role");

        if (userId == null || role == null)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "Please log in before accessing this resource."
            });
            return;
        }

        //Confirm that the session still matches the user's current database role
        var dbContext = context.HttpContext.RequestServices
            .GetRequiredService<RaceDayContext>();

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.UserID == userId.Value);

        if (user == null || user.Role != role)
        {
            session.Clear();

            context.Result = new UnauthorizedObjectResult(new
            {
                message = "Your session is invalid. Please log in again."
            });
            return;
        }

        //Enforce role restrictions on the server
        if (_requiredRole != null && role != _requiredRole)
        {
            context.Result = new ObjectResult(new
            {
                message = "You do not have permission to access this resource."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
