using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.Service;
using Frameset.Web.Model.System;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.WebMvc.Services.System.Impl
{
    public class SysResourceRoleServiceImpl : ServiceImpl<SysResourceRole, long>, ISysResourceRoleService
    {
        public bool SaveRoleResources(long roleId, List<long> resIds)
        {
            List<SysResourceRole> resRoles = Query().Where(r => r.RoleId == roleId && r.Status.Equals(Constants.VALID)).ToList();
            List<long> adds = [];
            List<long> dels = [];

            if (!resRoles.IsNullOrEmpty())
            {
                List<long> olds = resRoles.Select(x => x.ResId).ToList();
                adds = resIds.Except(olds).ToList();
                dels = olds.Except(resIds).ToList();
            }
            else
            {
                adds.AddRange(resIds);
            }
            try
            {
                dbContext.SetAutoCommit(false);
                if (!dels.IsNullOrEmpty())
                {
                    dbContext.Update<SysResourceRole>(u => u.RoleId == roleId && u.Status.Equals(Constants.VALID) && dels.Contains(u.ResId), () => new SysResourceRole { Status = Constants.INVALID });
                }
                if (!adds.IsNullOrEmpty())
                {
                    List<SysResourceRole> models = adds.Select(x => new SysResourceRole(roleId, x)).ToList();
                    dbContext.SaveEntities<SysResourceRole>(models);
                }
                dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw new ServiceException(ex.Message, ex);
            }
            finally
            {
                dbContext.ResetCommitStatus();
            }

        }
    }
}
