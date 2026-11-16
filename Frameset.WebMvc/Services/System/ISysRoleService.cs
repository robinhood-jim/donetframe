using Frameset.Core.Service;
using Frameset.Web.Model.User;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.QueryDto;

namespace Frameset.WebMvc.Services.System
{
    public interface ISysRoleService : IService<SysRole, long>
    {
        List<SysRole> Search(SysRoleQueryDto dto);
        bool SaveRole(SysRoleDto dto);
        bool ValidateRoleName(string roleCode);
    }
}
