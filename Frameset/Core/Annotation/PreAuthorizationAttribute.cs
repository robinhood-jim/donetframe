using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PreAuthorizationAttribute : Attribute
    {
        public List<string> Roles
        {
            get; set;
        }
        public List<string> PermissionKeys
        {
            get; set;
        }
        public PreAuthorizationAttribute(string roles)
        {
            Roles = roles.Split(',').ToList();
        }
        public PreAuthorizationAttribute(string roles, string permissions)
        {
            if (!roles.IsNullOrEmpty())
            {
                Roles = roles.Split(',').ToList();
            }
            PermissionKeys = permissions.Split(',').ToList();
        }
    }
}
