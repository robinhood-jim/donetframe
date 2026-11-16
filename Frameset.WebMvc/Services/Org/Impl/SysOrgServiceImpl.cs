using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Context;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Linq;
using Frameset.Core.Service;
using Frameset.Web.Model.Org;
using Frameset.Web.Model.User;
using Frameset.Web.Utils;
using Frameset.WebMvc.Common;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.QueryDto;
using Frameset.WebMvc.Vo;
using Microsoft.IdentityModel.Tokens;
using System.Data.Common;
using System.Diagnostics;

namespace Frameset.WebMvc.Services.Org.Impl
{
    [Service]
    public class SysOrgServiceImpl : ServiceImpl<SysOrg, long>, ISysOrgService
    {
        [AutoInject]
        private IRegionService regionService = null!;
        public List<SysOrgVO> QueryOrg(SysOrgDto dto)
        {

            Dictionary<long, List<SysOrgVO>> pidDict = GetSubDict();
            if (pidDict.TryGetValue(0, out List<SysOrgVO>? topList))
            {
                foreach (SysOrgVO vo in topList)
                {
                    RecrusiveDown(pidDict, vo);
                }
                return topList;
            }
            throw new WebException("you have not any org tree");
        }
        private void RecrusiveDown(Dictionary<long, List<SysOrgVO>> dict, SysOrgVO current)
        {
            if (dict.TryGetValue(current.Id, out List<SysOrgVO> childrens))
            {
                foreach (SysOrgVO vo in childrens)
                {
                    current.Children.Add(vo);
                    RecrusiveDown(dict, vo);
                }
            }
        }
        public WebPageDto<SysOrgUserVO> QueryOrgUsers(SysOrgUserDto dto)
        {
            Trace.Assert(dto != null, "");
            Dictionary<long, List<SysOrgVO>> pidDict = GetSubDict();
            List<long> pids = [];
            GetSubIds(pidDict, dto.Pid, pids);
            IQueryable<Employee> employees = GetDao().Query<Employee>();
            IQueryable<SysOrgEmployee> orgEmployees = GetDao().Query<SysOrgEmployee>();
            IQueryable<SysOrgUserVO>? queryable = null;
            if (string.Equals(dto.SelType, Constants.VALID, StringComparison.OrdinalIgnoreCase))
            {
                queryable = employees.Join(orgEmployees, e => e.Id, em => em.EmpId, (employees, orgEmployees) => new { employees, orgEmployees })
                    .Where(joined => pids.Contains(joined.orgEmployees.OrgId) && joined.employees.Status.Equals(Constants.VALID))
                    .Select(joined => new SysOrgUserVO
                    {
                        Id = joined.employees.Id,
                        Name = joined.employees.Name,
                        Address = joined.employees.Address,
                        BrithDay = joined.employees.BrithDay,
                        ContactPhone = joined.employees.ContactPhone,
                        CreditNo = joined.employees.CreditNo,
                        District = joined.employees.District,
                        Gender = joined.employees.Gender
                    }).Skip(Convert.ToInt32(dto.PageSize * (dto.CurrentPage - 1))).Take(Convert.ToInt32(dto.PageSize));
            }
            else
            {
                queryable = employees.Where(emp => !orgEmployees.Any(orgemp => orgemp.EmpId == emp.Id && pids.Contains(orgemp.OrgId)))
                    .Select(emp => new SysOrgUserVO
                    {
                        Id = emp.Id,
                        Name = emp.Name,
                        Address = emp.Address,
                        BrithDay = emp.BrithDay,
                        ContactPhone = emp.ContactPhone,
                        CreditNo = emp.CreditNo,
                        District = emp.District,
                        Gender = emp.Gender
                    })
                    .Skip(Convert.ToInt32(dto.PageSize * (dto.CurrentPage - 1))).Take(Convert.ToInt32(dto.PageSize));
            }
            var users = queryable?.ToList().Select(x =>
            {
                List<string> regionNames = regionService.GetRegionNames(x.District);
                if (!regionNames.IsNullOrEmpty())
                {
                    x.Province = regionNames[0];
                    x.City = regionNames[1];
                    x.District = regionNames[2];
                }
                return x;
            }).ToList();

            return new WebPageDto<SysOrgUserVO>
            {
                Total = (queryable.Provider as CustomQueryProvider).QueryCount,
                PageSize = dto.PageSize,
                CurrentPage = dto.CurrentPage,
                Records = users
            };

        }
        private void GetSubIds(Dictionary<long, List<SysOrgVO>> pidDict, long pid, List<long> ids)
        {
            if (!ids.Contains(pid))
            {
                ids.Add(pid);
            }

            if (pidDict.TryGetValue(pid, out List<SysOrgVO> orgs))
            {
                if (!orgs.IsNullOrEmpty())
                {
                    foreach (SysOrgVO vo in orgs)
                    {
                        GetSubIds(pidDict, vo.Id, ids);
                    }
                }
            }
        }
        private Dictionary<long, List<SysOrgVO>> GetSubDict()
        {
            ISecurityUtils securityUtils = RegServiceContext.GetBean<ISecurityUtils>();
            if (!securityUtils.IsLoginUserOrgAdmin() && !securityUtils.IsLoginUserSysAdmin())
            {
                throw new WebException("login user is not admin!");
            }
            long? tenantId = securityUtils.GetLoginTenant();
            List<SysOrg> orgs = Query().Where(o => o.Status.Equals(Constants.VALID)).WhereIf(tenantId != null && tenantId != 0, o => o.TenantId == tenantId).ToList();
            List<SysOrgVO> sysOrgs = orgs.Select(x => SysOrgVO.FromEntity(x)).ToList();
            return sysOrgs.GroupBy(x => x.Pid).ToDictionary(x => x.Key, x => x.ToList());
        }

        public int JoinOrg(HttpContext context, long orgId, List<long> userIds)
        {
            ISecurityUtils securityUtils = RegServiceContext.GetBean<ISecurityUtils>();
            Trace.Assert(IsLoginUserOrgAdmin(context, securityUtils, orgId) || securityUtils.IsLoginUserSysAdmin() || IsLoginUserOrgOwner(context, securityUtils, orgId), "");
            List<DbParameter> dbParameters = new();
            dbParameters.Add(GetDao().GetDialect().WrapParameter("ids", userIds));

            string executeSql = "select count(1) from t_sys_user_info where id in (@ids) and user_status='1'";
            int count = Execute(executeSql, dbParameters.ToArray());
            Trace.Assert(!userIds.IsNullOrEmpty() && userIds.Count == count, "");

            List<SysUserOrg> userOrgs = [];
            for (int i = 0; i < userIds.Count; i++)
            {
                SysUserOrg sysUserOrg = new SysUserOrg()
                {
                    OrgId = orgId,
                    UserId = userIds[i]
                };
                userOrgs.Add(sysUserOrg);
            }
            return dbContext.SaveEntities(userOrgs);

        }
        public int RemoveFromOrg(HttpContext context, long orgId, List<long> userIds)
        {
            ISecurityUtils securityUtils = RegServiceContext.GetBean<ISecurityUtils>();
            Trace.Assert(IsLoginUserOrgAdmin(context, securityUtils, orgId) || securityUtils.IsLoginUserSysAdmin() || IsLoginUserOrgOwner(context, securityUtils, orgId), "");
            List<DbParameter> dbParameters = new();
            dbParameters.Add(GetDao().GetDialect().WrapParameter("ids", userIds));
            string executeSql = "select count(1) from t_sys_user_info where id in (@ids) and user_status='1'";
            int count = Execute(executeSql, dbParameters.ToArray());
            Trace.Assert(!userIds.IsNullOrEmpty() && userIds.Count == count, "");
            TenantInfo info = GetTenantInfoByOrg(orgId);
            return dbContext.Update<SysUserOrg>(u => userIds.Contains(u.UserId) && u.OrgId == orgId && u.Status.Equals(Constants.VALID), () => new SysUserOrg { Status = Constants.INVALID });

        }
        public bool AssignAdmin(long orgId, long adminUserId)
        {
            return true;
        }
        public bool IsLoginUserOrgAdmin(HttpContext context, ISecurityUtils securityUtils, long orgId)
        {
            if (securityUtils.IsLoginUserOrgAdmin())
            {
                string? userId = securityUtils.GetLoginUser();
                Trace.Assert(!string.IsNullOrWhiteSpace(userId), "");
                TenantInfo info = GetTenantInfoByOrg(orgId);
                SingleFilterConditionBuilder builder = SingleFilterConditionBuilder.NewBuilder<TenantUser>();
                builder.AddEq(nameof(TenantUser.TenantId), info.Id).AddEq(nameof(TenantUser.UserId), Convert.ToInt64(userId));
                List<TenantUser> tenantUsers = dbContext.QueryByCondition<TenantUser>(builder.Build());
                if (!tenantUsers.IsNullOrEmpty() && tenantUsers[0].Type == (int)WebConstants.TENANT_TYPE.ORGADMIN)
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsLoginUserOrgOwner(HttpContext context, ISecurityUtils securityUtils, long orgId)
        {
            string? userId = securityUtils.GetLoginUser();
            Trace.Assert(!string.IsNullOrWhiteSpace(userId), "");
            SysUser user = dbContext.GetById<SysUser, long>(Convert.ToInt64(userId));
            if (user.EmployerId > 0)
            {
                Employer employer = dbContext.GetById<Employer, long>(user.EmployerId);
                Trace.Assert(employer != null, "");
                Dictionary<string, object> queryDict = [];
                queryDict.TryAdd("employerId", employer.Id);
                queryDict.TryAdd("orgId", orgId);
                int count = dbContext.ExecuteMapper("Frameset.Core.Query.Sys", "isOwnerOfOrg", queryDict);
                return count == 1;
            }
            return false;
        }
        private TenantInfo GetTenantInfoByOrg(long orgId)
        {
            List<TenantInfo> tenants = dbContext.QueryModelsByField<TenantInfo>(nameof(TenantInfo.OrgId), Constants.SqlOperator.EQ, [orgId]);
            Trace.Assert(!tenants.IsNullOrEmpty() && tenants.Count == 1, "org tenant not exist or not unique!");
            return tenants[0];
        }
    }
}
