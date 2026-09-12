using Microsoft.AspNetCore.Authorization;

namespace Frameset.Web.Handler
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string? Url
        {
            get; set;
        }
        public string? Codes
        {
            get; set;
        }
        public string? Roles
        {
            get; set;
        }
        public string[] AllowMethods
        {
            get; set;
        } = [];
        public PermissionRequirement(string roles)
        {
            Roles = roles;
        }
        public PermissionRequirement(string? roles, string? codes)
        {
            Roles = roles;
            Codes = codes;
        }
        public PermissionRequirement(string? url, string? roles, string? codes) : this(roles, codes)
        {
            Url = url;
        }
        public PermissionRequirement(string url, string codes, string[] allowMethods)
        {
            Url = url;
            Codes = codes;
            AllowMethods = allowMethods;
        }

    }
}
