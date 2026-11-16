using Frameset.Core.Service;
using Frameset.Web.Model.Org;

namespace Frameset.WebMvc.Services.Org
{
    public interface ITenantInfoService : IService<TenantInfo, long>
    {
        List<Dictionary<string, object>> GetUserTenants(long userId);
        bool ActivateEnterprise(long tenantId);
        bool Invite(long tenantId, List<long> empIds);
    }
}
