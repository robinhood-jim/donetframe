using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Context;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Service;
using Frameset.Core.Utils;
using Frameset.Web.Model.Org;
using Frameset.Web.Model.User;
using Frameset.Web.Utils;
using Frameset.WebMvc.Common;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.WebMvc.Services.Org.Impl
{
    [Service]
    public class EnterpriseServiceImpl : ServiceImpl<Enterprise, long>, IEnterpriseService
    {
        private Random random = new Random(10010);

        public bool RegisterEnterprise(Enterprise enterprise, Dictionary<string, object> inputDict)
        {
            ISecurityUtils securityUtils = RegServiceContext.GetBean<ISecurityUtils>();

            if (!securityUtils.IsLogin() && inputDict.TryGetValue("phoneNum", out object? phoneNum))
            {
                List<Employer> employers = dbContext.QueryModelsByField<Employer>(nameof(Employer.PhoneNum), Constants.SqlOperator.EQ, [phoneNum]);
                if (employers == null || employers.IsNullOrEmpty())
                {
                    ConvertUtil.FromInput(inputDict, out SysUser sysUser);
                    ConvertUtil.FromInput(inputDict, out Employer employer);
                    sysUser.UserStatus = Convert.ToInt32(WebConstants.AUDIT_TYPE.DRAFT);
                    employer.AuditStatus = Convert.ToInt32(WebConstants.AUDIT_TYPE.DRAFT);
                    enterprise.AuditStatus = Convert.ToInt32(WebConstants.AUDIT_TYPE.DRAFT);
                    string activeCode = StringUtils.GenerateRandomChar(random, 16);
                    dbContext.SetAutoCommit(false);
                    try
                    {
                        dbContext.SaveEntity(employer);
                        long employerId = employer.Id;
                        enterprise.OwnerId = employerId;
                        dbContext.SaveEntity(enterprise);
                        dbContext.SaveEntity(sysUser);
                        dbContext.SaveChanges();
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
                else
                {
                    throw new ConfigIncorrectException($"phoneNum {phoneNum} already register as Enterprise! Please login");
                }
            }
            else
            {
                string? userId = securityUtils.GetLoginUser();
                SysUser sysUser = dbContext.GetById<SysUser, long>(Convert.ToInt64(userId));
                Trace.Assert(sysUser != null && sysUser.EmployerId > 0, "");
                Employer employer = dbContext.GetById<Employer, long>(sysUser.EmployerId);
                int count = dbContext.Count<Enterprise>(e => e.OwnerId == Convert.ToInt64(userId) && e.Status.Equals(Constants.VALID));

            }
            return true;
        }
        public bool ActivateEnterprise(long enterpriseId, long? managerId = null)
        {
            Enterprise enterprise = dbContext.GetById<Enterprise, long>(enterpriseId);
            long? entManagerId = managerId;
            SysUser? employerUser = null;
            if (entManagerId == null || entManagerId <= 0)
            {
                Employer employer = dbContext.GetById<Employer, long>(enterprise.OwnerId);
                Trace.Assert(employer != null, "");
                SingleFilterConditionBuilder build = SingleFilterConditionBuilder.NewBuilder<SysUser>();
                build.AddEq(nameof(SysUser.EmployerId), enterprise.OwnerId).AddEq(nameof(SysUser.AccountType), WebConstants.ACCOUNT_TYPE.EMPLOYER);
                List<SysUser> users = dbContext.QueryByCondition<SysUser>(build.Build());
                if (!users.IsNullOrEmpty())
                {
                    employerUser = users[0];
                }
            }
            else
            {
                employerUser = dbContext.GetById<SysUser, long>(Convert.ToInt64(managerId));
            }

            //get enterprise employer 
            if (employerUser != null)
            {
                dbContext.SetAutoCommit(false);
                try
                {
                    SysOrg org = new SysOrg()
                    {
                        OrgName = enterprise.Name,
                        OrgCode = enterprise.Code,
                        OrgType = "3",
                        Pid = 0,
                        Level = 1,
                        EntId = enterpriseId
                    };
                    dbContext.SaveEntity(org);
                    long orgId = org.Id;
                    // add tenant
                    TenantInfo info = new TenantInfo()
                    {
                        TenantName = enterprise.Name,
                        TenantCode = enterprise.Code,
                        EntId = enterpriseId,
                        OrgId = orgId,
                        RegTime = DateTime.Now
                    };
                    dbContext.SaveEntity(info);
                    long tenantId = info.Id;

                    TenantUser tuser = new TenantUser()
                    {
                        UserId = employerUser.Id,
                        TenantId = tenantId,
                        Type = (int)WebConstants.TENANT_TYPE.OWNER,
                    };
                    dbContext.SaveEntity(tuser);
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
            else
            {
                throw new ConfigMissingException("employer does't have any valid account!");
            }

        }
    }
}
