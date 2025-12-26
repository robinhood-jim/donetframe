using Frameset.Common.Annotation;
using Frameset.Core.Repo;
using Frameset.Web.Model;
using Microsoft.AspNetCore.Http;

namespace Frameset.Serverless.Services
{
    public class TestService
    {
        [ServerlessFunc]
        public static object GetRole(HttpRequest request, HttpResponse response, long id, IBaseRepository<SysRole, long> repository)
        {
            SysRole role = repository.GetById(id);
            if (role != null)
            {
                return role;
            }
            else
            {
                return "NOT FOUND";
            }
        }
    }
}
