using Frameset.Web.Utils;
using Frameset.WebMvc.Common;
using System.Diagnostics;
using System.Security.Claims;

namespace Frameset.WebMvc.Utils
{
    public class SecurityUtils : ISecurityUtils
    {
        private IHttpContextAccessor httpContext;
        public SecurityUtils(IHttpContextAccessor _httpContext)
        {
            httpContext = _httpContext;
        }
        public bool IsLogin()
        {
            var identity = httpContext.HttpContext?.User != null ? httpContext.HttpContext.User?.Identity : null;
            return identity != null && identity.IsAuthenticated;
        }
        public string? GetLoginUser()
        {
            return httpContext.HttpContext?.User.Claims.Where(x => x.Type.Equals(ClaimTypes.NameIdentifier)).First().Value;
        }
        public List<string> GetUserPermissions()
        {
            List<string> permissions = [];
            Claim? claim = httpContext.HttpContext?.User.Claims.Where(x => x.Type.Equals("PermissionCodes")).First();
            if (claim != null)
            {
                permissions.AddRange(claim.Value.Split(","));
            }
            return permissions;
        }
        public bool IsLoginUserSysAdmin()
        {
            HttpContext? context = httpContext.HttpContext;
            Trace.Assert(context != null, "");
            return context.User.Identity != null && context.User.Identity.IsAuthenticated && context.User.IsInRole(WebConstants.ACCOUNT_TYPE.SYSADMIN.ToString());
        }
        public bool IsLoginUserOrgAdmin()
        {
            HttpContext? context = httpContext.HttpContext;
            Trace.Assert(context != null, "");
            return context.User.Identity != null && context.User.Identity.IsAuthenticated && context.User.IsInRole(WebConstants.ACCOUNT_TYPE.ORGADMIN.ToString());
        }

        public long? GetLoginTenant()
        {
            HttpContext? context = httpContext.HttpContext;
            Trace.Assert(context != null, "");
            Claim? claim = context.User.Claims.Where(x => x.Type.Equals("tenantId")).First();
            if (claim != null)
            {
                return Convert.ToInt64(claim.Value);
            }
            return null;
        }

        public bool IsLoginUserTenantAdmin(long? tenantId = null)
        {
            throw new NotImplementedException();
        }

        public bool IsLoginUserTenantOwner(long? tenantId = null)
        {
            throw new NotImplementedException();
        }
    }
}
