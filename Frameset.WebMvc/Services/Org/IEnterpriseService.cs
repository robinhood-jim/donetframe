using Frameset.Core.Service;
using Frameset.Web.Model.Org;

namespace Frameset.WebMvc.Services.Org
{
    public interface IEnterpriseService : IService<Enterprise, long>
    {
        bool ActivateEnterprise(long enterpriseId, long? managerId = null);
    }
}
