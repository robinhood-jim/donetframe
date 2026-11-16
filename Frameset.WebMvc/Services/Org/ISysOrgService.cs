using Frameset.Core.Service;
using Frameset.Web.Model.Org;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.QueryDto;
using Frameset.WebMvc.Vo;

namespace Frameset.WebMvc.Services.Org
{
    public interface ISysOrgService : IService<SysOrg, long>
    {
        int JoinOrg(HttpContext context, long orgId, List<long> userIds);
        int RemoveFromOrg(HttpContext context, long orgId, List<long> userIds);
        List<SysOrgVO> QueryOrg(SysOrgDto dto);
        WebPageDto<SysOrgUserVO> QueryOrgUsers(SysOrgUserDto dto);
    }
}
