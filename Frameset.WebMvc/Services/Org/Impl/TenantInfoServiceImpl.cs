using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Repo;
using Frameset.Core.Service;
using Frameset.Web.Model.Org;
using Frameset.Web.Model.User;
using Frameset.WebMvc.Common;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.WebMvc.Services.Org.Impl
{
    [Service]
    public class TenantInfoServiceImpl : ServiceImpl<TenantInfo, long>, ITenantInfoService
    {
        [AutoInject]
        private readonly IService<SysUser, long> userService = null!;

        public bool ActivateEnterprise(long tenantId)
        {
            TenantInfo info = GetById(tenantId, true);
            Trace.Assert(info != null, "");
            Enterprise enterprise = dbContext.GetById<Enterprise, long>(info.EntId);
            Employer employer = dbContext.GetById<Employer, long>(enterprise.OwnerId);
            Trace.Assert(employer != null, "");
            SingleFilterConditionBuilder build = SingleFilterConditionBuilder.NewBuilder<SysUser>();
            build.AddEq(nameof(SysUser.EmployerId), enterprise.OwnerId).AddEq(nameof(SysUser.AccountType), WebConstants.ACCOUNT_TYPE.EMPLOYER);
            List<SysUser> users = dbContext.QueryByCondition<SysUser>(build.Build());
            //get enterprise employer 
            if (!users.IsNullOrEmpty())
            {
                SysUser employerUser = users[0];
                RepositoryHelper.ExecuteInTransaction(dbContext.GetDao(), cmd =>
                {
                    TenantUser tuser = new TenantUser()
                    {
                        UserId = employerUser.Id,
                        TenantId = tenantId,
                        Type = (int)WebConstants.ACCOUNT_TYPE.ORGADMIN,
                    };
                    InsertSegment insertSegment = SqlUtils.GetInsertSegment(dbContext.GetDao(), tuser);
                    dbContext.GetDao().SaveEntity(cmd, tuser, insertSegment);

                    return 1;
                });


            }
            else
            {
                throw new ConfigMissingException("employer does't have any valid account!");
            }

            return true;
        }

        public List<Dictionary<string, object>> GetUserTenants(long userId)
        {
            SysUser user = userService.GetById(userId);
            if (user == null || !Constants.VALID.Equals(user.Status))
            {
                throw new ConfigMissingException("user doesn't exists or is forzen!");
            }
            if (WebConstants.ACCOUNT_TYPE.SYSADMIN.Equals(user.AccountType))
            {
                return [];
            }
            else
            {
                Dictionary<string, object> queryDict = [];
                queryDict.TryAdd("status", Constants.VALID);
                queryDict.TryAdd("userId", userId);
                List<Dictionary<string, object>>? tenants = QueryMapper("Frameset.Core.Query.Sys", "getUserTenants", queryDict).Cast<Dictionary<string, object>>().ToList();
                if (WebConstants.ACCOUNT_TYPE.ORGADMIN.Equals(user.AccountType) && tenants.IsNullOrEmpty())
                {
                    throw new ConfigMissingException($"ORGADMIN {user.UserName} doen't manage any orgranization");
                }
                else if (tenants.IsNullOrEmpty())
                {
                    throw new ConfigMissingException($"{user.UserName} doen't belong to any orgranization");
                }
                return tenants;
            }

        }

        public bool Invite(long tenantId, List<long> empIds)
        {
            throw new NotImplementedException();
        }
    }
}
