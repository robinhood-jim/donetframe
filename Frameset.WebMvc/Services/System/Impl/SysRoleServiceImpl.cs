using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Linq;
using Frameset.Core.Service;
using Frameset.Core.Utils;
using Frameset.Web.Model.User;
using Frameset.Web.Utils;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.QueryDto;

namespace Frameset.WebMvc.Services.System.Impl
{
    [Service]
    public class SysRoleServiceImpl : ServiceImpl<SysRole, long>, ISysRoleService
    {
        [AutoInject]
        private ISecurityUtils securityUtils = null!;

        public List<SysRole> Search(SysRoleQueryDto dto)
        {
            IQueryable<SysRole> query = Query();
            query = query.WhereIf(!string.IsNullOrWhiteSpace(dto.Status), u => u.Status.Equals(dto.Status))
                .WhereIf(!securityUtils.IsLoginUserSysAdmin(), u => u.TenantId == securityUtils.GetLoginTenant())
                .OrGroupIf(!string.IsNullOrWhiteSpace(dto.Code), g => g.Like(u => u.RoleCode, dto.Code).Like(u => u.RoleName, dto.Code));
            PageRecords(query, dto);
            List<SysRole> roles = query.ToList();
            CustomQueryProvider? cquery = query.Provider as CustomQueryProvider;
            if (cquery != null)
            {
                dto.Total = cquery.QueryCount;
            }
            return roles;
        }
        public bool SaveRole(SysRoleDto dto)
        {
            if (ValidateRoleName(dto.RoleCode))
            {
                SysRole role = new();
                BeanUtils.CopyProperties(dto, role);
                return SaveEntity(role);
            }
            return false;
        }
        public bool ValidateRoleName(string roleCode)
        {
            int count = Query().Count(u => u.RoleCode.Equals(roleCode) && u.Status.Equals(Constants.VALID));
            return count < 1;
        }
    }
}
