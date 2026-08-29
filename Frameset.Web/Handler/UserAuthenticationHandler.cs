using Frameset.Web.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Frameset.Web.Handler
{
    public class UserAuthenticationHandler : IAuthenticationHandler
    {
        private readonly ILogger _logger;
        private readonly IDistributedCache _distributedCache;
        private HttpContext _context;
        private AuthenticationScheme _scheme;
        public UserAuthenticationHandler(ILogger<UserAuthenticationHandler> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public Task<AuthenticateResult> AuthenticateAsync()
        {

            //从 cookie 获取token
            _context.Request.Cookies.TryGetValue("token", out string? token);
            if (token.IsNullOrEmpty())
            {
                //从 Header 头获取
                _context.Request.Headers.TryGetValue("Authorization", out StringValues tokenValues);
                if (!tokenValues.IsNullOrEmpty())
                {
                    token = tokenValues.First();
                }
            }
            if (!token.IsNullOrEmpty())
            {
                var tokenStr = _distributedCache.GetString(string.Format("{0}:{1}", "userToken", token));
                if (!tokenStr.IsNullOrEmpty())
                {
                    LoginUser? loginUser = JsonSerializer.Deserialize<LoginUser>(tokenStr);
                    ClaimsIdentity identity = new ClaimsIdentity("Ctm");
                    Dictionary<string, object> userDataDict = [];
                    userDataDict.TryAdd("permissions", loginUser.Permissions);
                    userDataDict.TryAdd("roles", loginUser.Roles);
                    identity.AddClaims(new List<Claim>() {
                        new Claim(ClaimTypes.Name,loginUser.UserName),
                        new Claim(ClaimTypes.NameIdentifier,loginUser.UserId.ToString()),
                        new Claim(ClaimTypes.MobilePhone,loginUser.MobilePhone),
                        new Claim(ClaimTypes.UserData,JsonSerializer.Serialize(userDataDict))
                    });
                    var claimsPrincipal = new ClaimsPrincipal(identity);
                    return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, null, _scheme.Name)));
                }
            }
            return Task.FromResult(AuthenticateResult.Fail("认证失败！"));

        }

        public Task ChallengeAsync(AuthenticationProperties? properties)
        {
            _context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(AuthenticationProperties? properties)
        {
            _context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _logger.LogInformation("begin Verify");
            _context = context;
            _scheme = scheme;
            return Task.CompletedTask;
        }
    }
}
