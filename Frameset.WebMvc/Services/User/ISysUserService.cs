using Frameset.Core.Service;
using Frameset.Web.Model.User;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.QueryDto;

namespace Frameset.WebMvc.Services.User
{
    public interface ISysUserService : IService<SysUser, long>
    {
        bool SaveUserInfo(SysUserDto dto, string password);
        bool UpdateUserInfo(SysUserDto dto);
        void Search(UserQuery query, out List<SysUser> user, out List<SysRole> roles, out Dictionary<long, RoleDto> roleDict);
    }
}
