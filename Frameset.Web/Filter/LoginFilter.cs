using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Frameset.Web.Filter
{
    public class LoginFilter : IAuthorizationFilter
    {

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = CreateJsonResponse(
                    401,                              // HTTP status code
                    "Unauthorized",                   // Error title
                    "Authentication is required to access this resource."  // Detailed message
                );
            }
        }
        private JsonResult CreateJsonResponse(int statusCode, string error, string message)
        {
            // Create an anonymous object representing the JSON payload
            var jsonPayload = new
            {
                Status = statusCode,  // HTTP status code (e.g., 401, 403)
                Error = error,        // Error type (e.g., "Unauthorized")
                Message = message     // Human-readable error message
            };
            // Return a JsonResult object with the payload and the appropriate HTTP status code
            return new JsonResult(jsonPayload)
            {
                StatusCode = statusCode
            };
        }
    }
}
