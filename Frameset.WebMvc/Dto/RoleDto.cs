using Frameset.Web.Model.User;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.WebMvc.Dto
{
    public class RoleDto
    {
        public long Id
        {
            get; set;
        }
        public string RoleName
        {
            get; set;
        } = string.Empty;
        public string RoleCode
        {
            get; set;
        } = string.Empty;
        public int Level
        {
            get; set;
        }
        public bool Enable
        {
            get; set;
        } = true;
        public long TenantId
        {
            get; set;
        }
        public static RoleDto FromDict(Dictionary<string, object> dict)
        {
            Trace.Assert(!dict.IsNullOrEmpty(), "");
            RoleDto dto = new();
            if (dict.TryGetValue("id", out object? idObj))
            {
                dto.Id = Convert.ToInt64(idObj);
            }
            if (dict.TryGetValue("name", out object? nameObj))
            {
                dto.RoleName = Convert.ToString(nameObj);
            }
            if (dict.TryGetValue("code", out object? codeObj))
            {
                dto.RoleCode = Convert.ToString(codeObj);
            }
            if (dict.TryGetValue("roleType", out object? typeObj))
            {
                dto.Level = Convert.ToInt32(typeObj);
            }
            if (dict.TryGetValue("tenantId", out object? tenantIdObj))
            {
                dto.TenantId = Convert.ToInt64(tenantIdObj);
            }
            return dto;
        }
        public static RoleDto FromVO(SysRole role)
        {
            Trace.Assert(role != null, "");
            RoleDto dto = new();
            dto.Id = role.Id;
            dto.RoleName = role.RoleName;
            dto.RoleCode = role.RoleCode;
            dto.Level = Convert.ToInt32(role.RoleType);
            return dto;
        }
    }
}
