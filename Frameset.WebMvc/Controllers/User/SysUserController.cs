using Frameset.Core.Common;
using Frameset.Core.Context;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Query;
using Frameset.Core.Utils;
using Frameset.Web.Controller;
using Frameset.Web.Model.User;
using Frameset.Web.Services;
using Frameset.Web.Utils;
using Frameset.WebMvc.Common;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.QueryDto;
using Frameset.WebMvc.Services.System;
using Frameset.WebMvc.Services.User;
using Frameset.WebMvc.Vo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Security.Claims;

namespace Frameset.WebMvc.Controllers.User
{
    [ApiController]
    [Route("system/user/")]
    public class SysUserController : AbstractController<ISysUserService, SysUser, long>
    {
        private IConfiguration _configuration;
        private string? _defaultPwd;
        private ISecurityUtils securityUtils;
        private IPasswordEncoder passwordEncoder;
        private ISysResourceService sysResourceService;


        public SysUserController(IConfiguration configuration, ISecurityUtils securityUtils, IPasswordEncoder passwordEncoder, ISysResourceService sysResourceService) : base()
        {
            _configuration = configuration;
            _defaultPwd = _configuration["userDefaultPwd"];
            this.securityUtils = securityUtils;
            this.passwordEncoder = passwordEncoder;
            this.sysResourceService = sysResourceService;
        }
        [HttpGet("list")]
        public IActionResult Search([FromQuery] UserQuery? query)
        {
            Trace.Assert(query != null, "");
            _service.Search(query, out List<SysUser> user, out List<SysRole> roles, out Dictionary<long, RoleDto> roleDict);

            return OutputMsg(new WebPageDto<SysUserVO>(query.Total, query.PageSize, query.CurrentPage, user.Select(x =>
            {
                SysUserVO dto = new SysUserVO();
                BeanUtils.CopyProperties(x, dto);
                dto.Status = x.Status.Equals(Constants.VALID);
                if (!x.UserRoles.IsNullOrEmpty())
                {
                    dto.Roles = x.UserRoles.Select(x => roleDict[x.RoleId]).ToList();
                }
                return dto;
            }).ToList()));
        }

        [HttpGet("{id}")]
        [Authorize("Permission.SysUser")]
        public IActionResult Get(long id)
        {
            return GetEntity(id);
        }
        [HttpPost("save")]
        public IActionResult Save([FromBody] SysUserDto dto)
        {
            if (_service.SaveUserInfo(dto, _defaultPwd))
            {
                return OutputMsg("OK");
            }
            return OutputErrMsg("");
        }
        [HttpPut("update")]
        [Authorize("Permission.SysUser")]
        public IActionResult Update([FromBody] SysUserDto dto)
        {
            if (_service.UpdateUserInfo(dto))
            {
                return OutputMsg("OK");
            }
            return OutputErrMsg("");
        }

        [Authorize("Permission.SysUser")]
        public IActionResult QueryPage([FromBody] PageQuery query)
        {
            Dictionary<string, object> queryParameter = query.Parameters;
            SingleFilterConditionBuilder builder = SingleFilterConditionBuilder.NewBuilder<SysUser>();
            builder.AddEq(nameof(SysUser.Status), Constants.VALID).AddFilter(StringUtils.IsNotNull(queryParameter["phone"]), nameof(SysUser.PhoneNum), Constants.SqlOperator.LIKE, [queryParameter["phone"].ToString()]);

            return new JsonResult(QueryPage<Dictionary<string, object>>(query));
        }
        [HttpDelete("delete")]
        [Authorize("Permission.SysUser")]
        public IActionResult Delete([FromBody] string ids)
        {
            return RemoveEnities(ids);
        }

        [HttpGet("listorg")]
        [Authorize("Permission.SysUser")]
        public IActionResult ListOrg()
        {
            if (Request.HttpContext.User.Identities != null)
            {
                var userId = Request.HttpContext.User.Claims.FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value;
                Trace.Assert(!string.IsNullOrWhiteSpace(userId), "");
                Dictionary<string, object> queryObj = [];
                queryObj.TryAdd("UserId", userId);
                var queryRs = DbContextFactory.GetContext().QueryMapper("Frameset.WebMvc", "listUserOrg", queryObj).Cast<Dictionary<string, object>>().ToList();
                return new JsonResult(queryRs);
            }
            return Unauthorized(new { error = "not login!" });
        }

        [HttpPost("changePassword")]
        public IActionResult ChangePwd([FromBody] Dictionary<string, object> postDict)
        {
            if (postDict.TryGetValue("newPassword", out object? newPwdObj))
            {
                var userIdObj = RegServiceContext.GetBean<ISecurityUtils>().GetLoginUser();
                long userId = Convert.ToInt64(userIdObj);
                SysUser user = DbContextFactory.GetContext().GetById<SysUser, long>(userId);
                if (!string.IsNullOrWhiteSpace(user.UserPassword))
                {

                    if (postDict.TryGetValue("originPwd", out object? orgPwdObj))
                    {
                        string? orgPwdStr = orgPwdObj.ToString();
                        Trace.Assert(!string.IsNullOrWhiteSpace(orgPwdStr), "");
                        if (!passwordEncoder.Match(user.UserPassword, orgPwdStr))
                        {
                            return OutputErrMsg("password mismatch!");
                        }
                    }
                    string? newPwd = newPwdObj.ToString();
                    Trace.Assert(!string.IsNullOrWhiteSpace(newPwd), "");
                    user.UserPassword = passwordEncoder.EncodeOrigin(newPwd).ToUpper();
                    user.UserStatus = Convert.ToInt32(WebConstants.AUDIT_TYPE.VERIFYIED);
                    if (DbContextFactory.GetContext().UpdateEntity<SysUser, long>(user))
                    {
                        return OutputMsg("密码修改成功!");
                    }
                }
            }
            return OutputErrMsg("缺少参数");
        }

        [HttpGet("active/{id}")]
        public IActionResult Activate(long id)
        {
            try
            {
                SysUser user = _service.GetById(id);
                if (string.Equals(user.Status, Constants.VALID, StringComparison.OrdinalIgnoreCase))
                {
                    return OutputMsg("账户已激活");
                }
                user.Status = Constants.VALID;
                return DbContextFactory.GetContext().UpdateEntity<SysUser, long>(user) ? OutputMsg("激活成功") : OutputErrMsg("失败");
            }
            catch (Exception ex)
            {
                return OutputErrMsg(ex.Message);
            }
        }

        [HttpGet("deactive/{id}")]
        public IActionResult DeActive(long id)
        {
            try
            {
                SysUser user = _service.GetById(id);
                if (string.Equals(user.Status, Constants.INVALID, StringComparison.OrdinalIgnoreCase))
                {
                    return OutputMsg("账户已禁用");
                }
                user.Status = Constants.VALID;
                return DbContextFactory.GetContext().UpdateEntity<SysUser, long>(user) ? OutputMsg("禁用成功") : OutputErrMsg("失败");
            }
            catch (Exception ex)
            {
                return OutputErrMsg(ex.Message);
            }
        }
        [HttpPost("{id}/permission")]
        public IActionResult AssingPermissions(long id, [FromBody] List<long> resIds)
        {

            if (sysResourceService.UpdateUserPermissions(id, securityUtils.GetLoginTenant(), resIds))
            {
                return OutputMsg("OK");
            }
            return OutputErrMsg("");
        }

        public IActionResult AuditUser(long id, int userStatus, string message)
        {
            try
            {
                List<SysUser> users = _service.Query().Where(u => u.Id == id && u.UserStatus == Convert.ToInt32(WebConstants.AUDIT_TYPE.DRAFT)).ToList();
                if (!users.IsNullOrEmpty())
                {
                    _service.GetContext().Update<SysUser>(u => u.Id == id, () => new SysUser { UserStatus = userStatus });

                    if (userStatus > Convert.ToInt32(WebConstants.AUDIT_TYPE.DRAFT))
                    {

                    }


                }
            }
            catch (Exception ex)
            {
                throw new WebException(ex.Message, ex);
            }
            return null;
        }
    }
}
