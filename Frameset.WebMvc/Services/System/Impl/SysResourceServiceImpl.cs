using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Service;
using Frameset.Core.Utils;
using Frameset.Web.Model.System;
using Frameset.Web.Model.User;
using Frameset.WebMvc.Common;
using Frameset.WebMvc.Dto;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.WebMvc.Services.System.Impl
{
    [Service]
    public class SysResourceServiceImpl : ServiceImpl<SysResource, long>, ISysResourceService
    {
        public List<SysResourceDto> Search(Dictionary<string, object>? dict)
        {
            bool useQuery = false;
            List<SysResource> menus = QueryModeslByField(nameof(SysResource.Status), Constants.SqlOperator.EQ, [Constants.VALID]);
            Dictionary<long, List<SysResource>> pidMap = menus.GroupBy(x => x.Pid).ToDictionary(x => x.Key, x => x.ToList());
            SingleFilterConditionBuilder builder = SingleFilterConditionBuilder.NewBuilder<SysResource>();
            if (dict != null)
            {
                if (StringUtils.IsDictNullWithReturn(dict, "type", out string type))
                {
                    useQuery = true;
                    builder.AddFilter(nameof(SysResource.ResType), Constants.SqlOperator.EQ, [type]);
                }
                if (StringUtils.IsDictNullWithReturn(dict, "name", out string name))
                {
                    useQuery = true;
                    builder.AddLike(true, nameof(SysResource.Code), name);
                }
            }
            if (!useQuery)
            {
                builder.AddEq(nameof(SysResource.Pid), 0);
            }
            List<SysResource> querys = QueryByCondition(builder.Build());
            return querys.Select(x =>
            {
                SysResourceDto dto = new SysResourceDto();
                BeanUtils.CopyProperties(x, dto);
                dto.ResName = x.Name;
                dto.Sort = x.SeqNo;
                RecursiveTree(pidMap, dto);
                return dto;
            }).ToList();

        }
        private void RecursiveTree(Dictionary<long, List<SysResource>> pidMap, SysResourceDto dto)
        {
            if (pidMap.TryGetValue(dto.Id, out List<SysResource>? list))
            {
                List<SysResourceDto> rList = list.Select(x =>
                {
                    SysResourceDto dto = new();
                    BeanUtils.CopyProperties(x, dto);
                    dto.ResName = x.Name;
                    dto.Sort = x.SeqNo;
                    RecursiveTree(pidMap, dto);
                    return dto;
                }).ToList();
                dto.Children.AddRange(rList);
            }
        }
        public List<long> GetPermissions(long userId, long? tenantId = null)
        {
            return GetPermissions<long>(userId, tenantId, "id");
        }
        public Tuple<List<string>, List<string>> GetRolesAndPermissions(long userId)
        {
            //获取 Permission
            Dictionary<string, object> dict = [];
            dict.TryAdd("userId", userId);
            List<Dictionary<string, object>> permissions = QueryMapper("Frameset.Core.Query.Sys", "getResPermission", dict, ["userId", "tenantId"]).Cast<Dictionary<string, object>>().ToList();
            List<Dictionary<string, object>> groupList = permissions.GroupBy(f => f["id"]).Select(group => { Dictionary<string, object> dict = []; dict.TryAdd("Key", group.Key); dict.TryAdd("Values", group.ToList()); return dict; }).ToList();
            List<string> permissionCodes = [];
            foreach (Dictionary<string, object> group in groupList)
            {
                long id = Convert.ToInt64(group["Key"].ToString());
                List<Dictionary<string, object>> values = (List<Dictionary<string, object>>)group["Values"];
                var selected = values.OrderByDescending(f => f["id"] + "|" + f["assignType"]).First();

                if (selected.TryGetValue("assignType", out object? assignType) && !Equals(assignType, WebConstants.RESOURCE_DENIED) && selected.TryGetValue("code", out object? code))
                {
                    string? codeStr = code?.ToString();
                    if (!string.IsNullOrWhiteSpace(codeStr))
                    {
                        permissionCodes.Add(codeStr);
                    }
                }
            }
            //获取roles
            List<Dictionary<string, object>> roles = QueryMapper("Frameset.Core.Query.Sys", "getUserRoles", dict).Cast<Dictionary<string, object>>().ToList();
            return Tuple.Create(roles.Select(x => x["code"].ToString()).ToList(), permissionCodes);
        }
        public List<string> GetPermissionCodes(long userId, long? tenantId)
        {
            return GetPermissions<string>(userId, tenantId, "code");
        }
        private List<T> GetPermissions<T>(long userId, long? tenantId, string selectColumn)
        {
            Dictionary<string, object> dict = [];
            dict.TryAdd("userId", userId);
            if (tenantId != null && tenantId != 0)
            {
                dict.TryAdd("tenantId", tenantId);
            }
            List<Dictionary<string, object>> permissions = QueryMapper("Frameset.Core.Query.Sys", "getResPermission", dict, ["userId", "tenantId"]).Cast<Dictionary<string, object>>().ToList();
            List<Dictionary<string, object>> groupList = permissions.GroupBy(f => f["id"]).Select(group => { Dictionary<string, object> dict = []; dict.TryAdd("Key", group.Key); dict.TryAdd("Values", group.ToList()); return dict; }).ToList();
            List<T> permissionCodes = [];
            foreach (Dictionary<string, object> group in groupList)
            {
                long id = Convert.ToInt64(group["Key"].ToString());
                List<Dictionary<string, object>> values = (List<Dictionary<string, object>>)group["Values"];
                var selected = values.OrderByDescending(f => f["id"] + "|" + f["assignType"]).First();

                if (selected.TryGetValue("assignType", out object? assignType) && !Equals(assignType, WebConstants.RESOURCE_DENIED) && selected.TryGetValue(selectColumn, out object? code))
                {
                    string? codeStr = code?.ToString();
                    if (!string.IsNullOrWhiteSpace(codeStr))
                    {
                        permissionCodes.Add((T)Convert.ChangeType(codeStr, typeof(T)));
                    }
                }
            }
            return permissionCodes;
        }
        public List<Dictionary<string, object>> QueryUserPermission(SysUser user, long? tenantId)
        {
            long? useTenantId = tenantId;
            if (tenantId == null || tenantId == 0 || WebConstants.ACCOUNT_TYPE.SYSADMIN.Equals(user.AccountType))
            {
                useTenantId = null;
            }
            Dictionary<string, object> queryParameter = [];
            queryParameter.TryAdd("userId", user.Id);
            if (useTenantId != null)
            {
                queryParameter.TryAdd("tenantId", useTenantId);
            }
            List<Dictionary<string, object>> list = QueryMapper("Frameset.Core.Query.Sys", "getResPermission", queryParameter).Cast<Dictionary<string, object>>().ToList();
            return list;
        }
        public bool UpdateUserPermissions(long userId, long? tenantId, List<long> permissions)
        {
            SysUser user = dbContext.GetById<SysUser, long>(userId);
            List<long> currentpermissions = GetPermissions<long>(userId, tenantId, "id");
            List<long> deletes = currentpermissions.Except(permissions).ToList();
            List<long> adds = permissions.Except(currentpermissions).ToList();
            if (deletes.IsNullOrEmpty() && adds.IsNullOrEmpty())
            {
                return false;
            }
            dbContext.SetAutoCommit(false);
            try
            {
                /*
                if (deletes.IsNullOrEmpty())
                {
                    dbContext.Update<SysResourceUser>(u => u.UserId == userId && u.Status.Equals(Constants.VALID) && deletes.Contains(u.ResId), u => new SysResourceUser { Status = Constants.INVALID });
                }
                if (!adds.IsNullOrEmpty())
                {
                    List<SysResourceUser> list = [];
                    foreach(long resId in adds)
                    {
                        list.Add(new SysResourceUser { UserId = userId, ResId = resId, AssingType = Convert.ToInt32(WebConstants.ASSIGN_TYPE.ASSING) });
                    }
                    dbContext.SaveEntities<SysResourceUser>(list);
                }*/

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
    }
}
