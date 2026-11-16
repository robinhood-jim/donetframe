using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Context;
using Frameset.Core.Dao.Utils;
using Frameset.Web.Controller;
using Frameset.Web.Model.System;
using Frameset.Web.Model.User;
using Frameset.Web.Utils;
using Frameset.WebMvc.Common;
using Frameset.WebMvc.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Security.Claims;

namespace Frameset.WebMvc.Controllers
{
    [Route("/")]
    public class IndexController : AbstractControllerBase
    {
        [AutoInject]
        private ISecurityUtils securityUtils = null!;
        [HttpGet("home/count")]
        public ActionResult StatCount()
        {
            Dictionary<string, object> dict = [];
            dict.TryAdd("inventoryValue", 1000);
            Dictionary<string, object> todayDict = [];
            todayDict.TryAdd("orderCount", 20);
            todayDict.TryAdd("saleCount", 1111);
            todayDict.TryAdd("costCount", 1000);
            todayDict.TryAdd("profit", 1000);
            todayDict.TryAdd("inventoryValue", 1000);
            dict.TryAdd("today", todayDict);
            return OutputMsg(dict);
        }
        [HttpGet("getInfo/")]
        public ActionResult GetUserInfo()
        {
            var userId = securityUtils.GetLoginUser();
            Trace.Assert(userId != null, "");
            SysUser sysUser = DbContextFactory.GetContext().GetById<SysUser, long>(Convert.ToInt64(userId));
            Dictionary<string, object> dict = new();
            dict.TryAdd("userId", userId);
            List<Dictionary<string, object>> roles = DbContextFactory.GetContext().QueryMapper("Frameset.Core.Query.Sys", "getUserRoles", dict).Cast<Dictionary<string, object>>().ToList();
            dict.Remove("userId");
            dict.TryAdd("info", LoginUserDto.FromSysUser(sysUser));
            if (!roles.IsNullOrEmpty())
            {
                dict.TryAdd("roles", roles.Select(x => RoleDto.FromDict(x)));
            }
            List<string> permissions = securityUtils.GetUserPermissions();
            dict.TryAdd("permissions", permissions.Select(x =>
            {
                Dictionary<string, string> dict1 = [];
                dict1.TryAdd("permission", x);
                return dict1;
            }));
            return OutputMsg(dict);
        }
        [HttpGet("getRouters")]
        [Authorize]
        public ActionResult GetRouter()
        {
            var userId = Request.HttpContext.User.Claims.Where(x => x.Type.Equals(ClaimTypes.NameIdentifier)).First().Value;
            Dictionary<string, object> queryDict = [];
            queryDict.TryAdd("userId", Convert.ToInt64(userId));

            SingleFilterConditionBuilder conditionBuilder = SingleFilterConditionBuilder.NewBuilder<SysResource>();
            conditionBuilder.AddFilter(nameof(SysResource.ResType), Constants.SqlOperator.IN, new object[] { ResourceConstants.RESOURCE_DIR, ResourceConstants.RESOURCE_MENU });
            conditionBuilder.AddEq("Status", Constants.VALID);
            List<SysResource> aviabaleResources = DbContextFactory.GetContext().QueryByCondition<SysResource>(conditionBuilder.Build());
            List<RouterDto> routerDtos = aviabaleResources.Select(x => RouterDto.FromVo(x)).ToList();
            Dictionary<long, RouterDto> dtoDict = routerDtos.ToDictionary(x => x.Id, x => x);
            RouterDto root = new();
            dtoDict.TryAdd(0, root);
            List<Dictionary<string, object>> permissions = DbContextFactory.GetContext().QueryMapper("Frameset.Core.Query.Sys", "getResPermission", queryDict, ["userId", "tenantId"]).Cast<Dictionary<string, object>>().ToList();
            Dictionary<long, int> readDict = [];
            if (!permissions.IsNullOrEmpty())
            {
                Dictionary<long, List<RouterDto>> parentDict = permissions.Select(x => dtoDict[(long)x["id"]]).ToList().GroupBy(x => x.Pid).ToDictionary(group => group.Key, group => group.ToList());
                List<RouterDto> rootList = parentDict[0];
                foreach (RouterDto router in rootList)
                {
                    if (!readDict.TryGetValue(router.Id, out _) && !string.Equals(router.AssignType, WebConstants.RESOURCE_DENIED, StringComparison.OrdinalIgnoreCase))
                    {
                        if (dtoDict.TryGetValue(router.Pid, out RouterDto? pRouter))
                        {
                            if (pRouter.Children.IsNullOrEmpty())
                            {
                                pRouter.Children = [];
                            }
                            if (dtoDict.TryGetValue(router.Id, out RouterDto? currentRouter))
                            {
                                pRouter?.Children?.Add(currentRouter);
                                DoScanChildren(dtoDict, parentDict, router, readDict);
                            }
                        }
                        readDict.TryAdd(router.Id, 1);
                    }
                }
            }
            Trace.Assert(!dtoDict[0].Children.IsNullOrEmpty(), "");
            return OutputMsg(dtoDict[0].Children);
        }
        private void DoScanChildren(Dictionary<long, RouterDto> dtoDict, Dictionary<long, List<RouterDto>> pDict, RouterDto current, Dictionary<long, int> readDict)
        {
            if (pDict.TryGetValue(current.Id, out List<RouterDto>? childs) && !childs.IsNullOrEmpty())
            {
                childs.Sort((x, y) => x.SeqNo.CompareTo(y.SeqNo));
                foreach (RouterDto router in childs)
                {
                    if (!readDict.TryGetValue(router.Id, out _) && !string.Equals(router.AssignType, WebConstants.RESOURCE_DENIED, StringComparison.OrdinalIgnoreCase))
                    {
                        if (dtoDict.TryGetValue(router.Pid, out RouterDto? pRouter))
                        {
                            if (pRouter.Children.IsNullOrEmpty())
                            {
                                pRouter.Children = [];
                            }
                            if (dtoDict.TryGetValue(router.Id, out RouterDto? currentRouter))
                            {
                                pRouter?.Children?.Add(currentRouter);
                                DoScanChildren(dtoDict, pDict, router, readDict);
                            }
                        }
                        readDict.TryAdd(router.Id, 1);
                    }
                }
            }

        }
    }

}
