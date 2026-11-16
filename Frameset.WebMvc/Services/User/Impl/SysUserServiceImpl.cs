using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Service;
using Frameset.Core.Utils;
using Frameset.Web.Model.User;
using Frameset.Web.Services;
using Frameset.WebMvc.Common;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.QueryDto;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.WebMvc.Services.User.Impl
{
    [Service]
    public class SysUserServiceImpl : ServiceImpl<SysUser, long>, ISysUserService
    {
        [AutoInject]
        private IPasswordEncoder passwordEncoder = null!;
        public void Search(UserQuery query, out List<SysUser> user, out List<SysRole> roles, out Dictionary<long, RoleDto> roleDict)
        {
            Dictionary<string, object> queryParameter = query.Parameters;

            SingleFilterConditionBuilder builder = SingleFilterConditionBuilder.NewBuilder<SysUser>();
            builder.AddEq(true, nameof(SysUser.Status), query.Status ? Constants.VALID : Constants.INVALID)
                .AddFilter(!string.IsNullOrWhiteSpace(query.Phone), nameof(SysUser.PhoneNum), Constants.SqlOperator.LIKE, [query.Phone])
                .AddEq(!string.IsNullOrWhiteSpace(query.AccountType), nameof(SysUser.AccountType), query.AccountType)
                .AddAnd(!string.IsNullOrWhiteSpace(query.Name), x =>
                {
                    FilterCondition cond1 = builder.Like(nameof(SysUser.UserName), query.Name);
                    FilterCondition cond2 = builder.Like(nameof(SysUser.NickName), query.Name);
                    x.Conditions.Add(builder.Or([cond1, cond2]));
                });
            user = QueryByCondition(builder.Build(), query);
            IQueryable<SysRole> roleQuery = GetDao().Query<SysRole>();
            roles = roleQuery.Where(r => r.Status.Equals(Constants.VALID)).ToList();
            roleDict = roles.Select(r => new RoleDto { Id = r.Id, RoleName = r.RoleName, RoleCode = r.RoleCode }).ToDictionary(x => x.Id, x => x);

        }

        public bool SaveUserInfo(SysUserDto dto, string password)
        {
            SysUser user = new();
            BeanUtils.CopyProperties(dto, user);
            user.Status = dto.Status ? Constants.VALID : Constants.INVALID;
            user.UserPassword = passwordEncoder.EncodeOrigin(password);
            user.UserStatus = Convert.ToInt32(WebConstants.AUDIT_TYPE.DRAFT);
            dbContext.SetAutoCommit(false);
            try
            {
                dbContext.SaveEntity(user);
                long userId = user.Id;
                List<SysUserRole> userRoles = [];
                foreach (long roleId in dto.Roles)
                {
                    userRoles.Add(new SysUserRole { RoleId = roleId, UserId = userId });
                }
                dbContext.SaveEntities(userRoles);
                dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw new BaseSqlException(ex.Message, ex);
            }
            finally
            {
                dbContext.ResetCommitStatus();
            }

        }
        public bool UpdateUserInfo(SysUserDto dto)
        {
            SysUser user = new();
            BeanUtils.CopyProperties(dto, user);
            SysUser origin = GetById(user.Id);
            List<long> originRoles = origin.UserRoles.Select(x => x.RoleId).ToList();
            List<long> diffs = originRoles.Except(dto.Roles).ToList();
            dbContext.SetAutoCommit(false);
            bool updateable = false;
            try
            {
                if (dbContext.UpdateEntity<SysUser, long>(user))
                {
                    updateable = true;
                }
                if (!diffs.IsNullOrEmpty() && !dto.Roles.IsNullOrEmpty())
                {
                    dbContext.Update<SysUserRole>(u => dto.Roles.Contains(u.RoleId) && u.UserId == user.Id && u.Status.Equals(Constants.VALID), () => new SysUserRole { Status = Constants.INVALID });
                    List<SysUserRole> userRoles = [];
                    foreach (long roleId in dto.Roles)
                    {
                        userRoles.Add(new SysUserRole { UserId = user.Id, RoleId = roleId });
                    }
                    dbContext.SaveEntities(userRoles);
                    updateable = true;
                }
                if (updateable)
                {
                    dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                dbContext.ResetCommitStatus();
            }
            return updateable;
        }
        public bool Register(SysUserDto dto)
        {
            Trace.Assert(!string.IsNullOrWhiteSpace(dto.PhoneNum), "");
            int count = dbContext.Count<SysUser>(u => u.PhoneNum.Equals(dto.PhoneNum) && u.Status.Equals(Constants.VALID));
            if (count > 0)
            {
                throw new DuplicatedException($"phoneNum {dto.PhoneNum} already register!");
            }
            SysUser sysUser = new();
            BeanUtils.CopyProperties(dto, sysUser);
            sysUser.UserStatus = Convert.ToInt32(WebConstants.AUDIT_TYPE.DRAFT);
            dbContext.SaveEntity(sysUser);
            return true;
        }
        public bool AuditUser(long userId, int userState)
        {
            return true;
        }
    }
}
