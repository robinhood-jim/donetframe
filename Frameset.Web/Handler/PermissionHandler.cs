using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Security.Claims;

namespace Frameset.Web.Handler
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var httpContext = context.Resource as DefaultHttpContext;
            Trace.Assert(httpContext != null, "");
            var resource = httpContext.Request.Path;
            string method = httpContext.Request.Method.ToLower();
            var Roles = context.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(x => x.Value).ToList();
            var permissionCodes = context.User.Claims.Where(c => c.Type == "PermissionCodes");
            List<string> permissions = permissionCodes.IsNullOrEmpty() ? [] : new(permissionCodes.First().Value.Split(','));
            bool fit = true;
            bool permit = false;
            if (!string.IsNullOrWhiteSpace(requirement.Url))
            {
                fit = resource.ToString().StartsWith(requirement.Url);
            }
            if (fit)
            {
                if (!requirement.AllowMethods.IsNullOrEmpty())
                {
                    fit = requirement.AllowMethods.Select(x => string.Equals(x, method, StringComparison.OrdinalIgnoreCase)).Any();
                }
                if (fit && !string.IsNullOrWhiteSpace(requirement.Roles) && !Roles.IsNullOrEmpty())
                {
                    List<string> fitRoles = new(requirement.Roles.Split(','));
                    permit = fitRoles.Select(x => Roles.Contains(x)).Any();
                }
                if (fit && !permit && !string.IsNullOrWhiteSpace(requirement.Codes) && !permissions.IsNullOrEmpty())
                {
                    List<string> fitCodes = new(requirement.Codes.Split(','));
                    permit = fitCodes.Select(x => permissions.Contains(x)).Any();
                }
            }
            if (fit && permit)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }
    }
}
