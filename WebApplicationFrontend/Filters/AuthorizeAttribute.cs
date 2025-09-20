/*
 * File: AuthorizeAttribute.cs
 * Description: Custom authorization filter for role-based access control
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using WebApplicationFrontend.Services;

namespace WebApplicationFrontend.Filters
{
    /// <summary>
    /// Custom authorization attribute for role-based access control
    /// </summary>
    public class AuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[]? _allowedRoles;

        public AuthorizeAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        /// <summary>
        /// Check if user is authorized before action execution
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var userJson = session.GetString("CurrentUser");

            // Check if user is logged in
            if (string.IsNullOrEmpty(userJson))
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            // If specific roles are required, check user role
            if (_allowedRoles != null && _allowedRoles.Length > 0)
            {
                try
                {
                    var user = JsonSerializer.Deserialize<Services.WebUserLoginResponse>(userJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (user == null || !_allowedRoles.Contains(user.Role))
                    {
                        context.Result = new ForbidResult();
                        return;
                    }
                }
                catch
                {
                    context.Result = new RedirectToActionResult("Login", "Account", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}