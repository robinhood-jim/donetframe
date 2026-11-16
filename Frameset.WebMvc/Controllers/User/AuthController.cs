using Frameset.Web.Services;
using Frameset.Web.Utils;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.Services;
using Frameset.WebMvc.Services.System;
using Lazy.Captcha.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text;

namespace Frameset.WebMvc.Controllers.User;

[ApiController]
[Route("auth/")]
public class AuthController : WebBasedController
{
    private readonly IConfiguration configuration;
    private readonly IDatabase _redisDb;
    private readonly IPasswordEncoder passwordEncoder;

    private readonly byte[] jwtSecurity;
    private bool verifyCaptcha = false;

    private readonly ICaptcha? captcha = null!;
    private readonly ISysResourceService sysResourceService;
    private readonly ILoginService loginService;
    private readonly ISecurityUtils securityUtils;


    public AuthController(IConfiguration configuration, IConnectionMultiplexer redis, IPasswordEncoder encoder, ISecurityUtils securityUtils, ILoginService loginService, ISysResourceService sysResourceService)
    {
        this.configuration = configuration;
        _redisDb = redis.GetDatabase();
        passwordEncoder = encoder;
        jwtSecurity = Encoding.ASCII.GetBytes(this.configuration["Jwt:Secret"] ?? "123456");
        this.sysResourceService = sysResourceService;
        this.loginService = loginService;
        this.securityUtils = securityUtils;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            Dictionary<string, object> retDict = await loginService.DoLogin(Request.HttpContext, passwordEncoder, request, jwtSecurity, _redisDb, verifyCaptcha, captcha);
            return OutputMsg(retDict);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
    [HttpGet("logout")]
    public async Task<ActionResult> LogOut()
    {
        string? sessionId = CookieUtils.GetCookie(Request, "sessionKey");
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _redisDb.KeyDeleteAsync($"user:jwt:{sessionId}");
        }
        CookieUtils.DeleteCookie(Response, "sessionKey");
        CookieUtils.DeleteCookie(Response, "AuthToken");
        return OutputMsg("登出成功");

    }

    [HttpGet("right/{userId}")]
    [Authorize("Permission.ADMIN")]
    public ActionResult GetRight(long userId)
    {
        Tuple<List<string>, List<string>> tuple = sysResourceService.GetRolesAndPermissions(userId);
        return new JsonResult(tuple);
    }
    [HttpPost("selectTenant")]
    [Authorize]
    public IActionResult SelectTenant([FromBody] Dictionary<string, string> bodyDict)
    {
        string? userId = securityUtils.GetLoginUser();
        Trace.Assert(!string.IsNullOrWhiteSpace(userId), "");
        if (bodyDict.TryGetValue("sessionKey", out string? sessionKey) && bodyDict.TryGetValue("tenantId", out string? tenantId))
        {
            List<string> permissions = sysResourceService.GetPermissionCodes(Convert.ToInt64(userId), Convert.ToInt64(tenantId));
            loginService.SwitchTenant(Request.HttpContext, jwtSecurity, _redisDb, Convert.ToInt64(tenantId), permissions, sessionKey, out string tokenString);
            Dictionary<string, object> retDict = [];
            retDict.TryAdd("token", tokenString);
            return OutputMsg(retDict);
        }
        else
        {
            return OutputErrMsg("sessionKey or tenantId required!");
        }
    }


}