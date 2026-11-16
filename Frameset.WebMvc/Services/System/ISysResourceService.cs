using Frameset.Core.Service;
using Frameset.Web.Model.System;
using Frameset.Web.Model.User;
using Frameset.WebMvc.Dto;

namespace Frameset.WebMvc.Services.System
{
    public interface ISysResourceService : IService<SysResource, long>
    {
        List<SysResourceDto> Search(Dictionary<string, object>? dict);
        Tuple<List<string>, List<string>> GetRolesAndPermissions(long userId);
        List<long> GetPermissions(long userId, long? tenantId = null);
        List<string> GetPermissionCodes(long userId, long? tenantId = null);
        bool UpdateUserPermissions(long userId, long? tenantId, List<long> permissions);
        List<Dictionary<string, object>> QueryUserPermission(SysUser user, long? tenantId);
    }
}
