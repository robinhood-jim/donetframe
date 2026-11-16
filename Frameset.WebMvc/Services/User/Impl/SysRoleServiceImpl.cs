using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Service;
using Frameset.Web.Model.System;
using Frameset.Web.Model.User;

namespace Frameset.WebMvc.Services.User.Impl
{
    [Service]
    public class SysRoleServiceImpl : ServiceImpl<SysRole, long>, ISysRoleService
    {
        public bool SaveRolesRights(string[] ids, string resId)
        {
            try
            {
                SetAutoCommit(false);
                dbContext.RemoveByFields<SysResourceRole, long>(nameof(SysResourceRole.ResId),
                    Constants.SqlOperator.EQ, [Convert.ToInt64(resId)]);
                for (int i = 0; i < ids.Length; i++)
                {
                    SysResourceRole resourceRole = new()
                    {
                        ResId = Convert.ToInt64(resId),
                        RoleId = Convert.ToInt64(ids[i]),
                    };
                    dbContext.SaveEntity(resourceRole);
                }
                dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                ResetCommitStatus();
            }
            return false;
        }

    }
}
