using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Context;
using Frameset.Core.Exceptions;
using Frameset.Core.Utils;
using Frameset.Web.Model.User;
using Frameset.Web.Services;
using Frameset.WebMvc.Common;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.Services.Org;
using Frameset.WebMvc.Services.System;
using Lazy.Captcha.Core;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Frameset.WebMvc.Services.Impl
{
    [Service]
    public class LoginServiceImpl : ILoginService
    {
        [AutoInject]
        private ISysResourceService sysResourceService = null!;
        public async Task<Dictionary<string, object>> DoLogin(HttpContext context, IPasswordEncoder passwordEncoder, LoginRequest request, byte[] jwtSecurity, IDatabase _redisDb, bool verifyCaptcha, ICaptcha? captcha)
        {
            try
            {
                Trace.Assert(!string.IsNullOrWhiteSpace(request.UserAccount) || string.IsNullOrWhiteSpace(request.Password), "usename or password required!");
                IQueryable<SysUser> linqQuery = sysResourceService.GetDao().Query<SysUser>();
                int userStatus = Convert.ToInt32(WebConstants.AUDIT_TYPE.DRAFT);
                List<SysUser> users = linqQuery.Where(s => (s.UserAccount.Equals(request.UserAccount) || s.PhoneNum.Equals(request.UserAccount)) && s.Status.Equals(Constants.VALID) && s.UserStatus > userStatus).ToList();

                if (users.Count == 0)
                {
                    throw new NotFoundException("username or phonenum not found!");
                }
                if (users.Count > 1)
                {
                    throw new NotUniqueException("usename or phonenum not unique");
                }
                Dictionary<string, object> retDict = [];
                SysUser user = users[0];
                if (!passwordEncoder.Match(user.UserPassword, request.Password))
                {
                    throw new ConfigIncorrectException("password mismatch!");
                }

                if (verifyCaptcha)
                {
                    Trace.Assert(captcha != null, "missing captcha config");
                    if (!captcha.Validate(request.VerifyId, request.VerifyCode))
                    {
                        throw new ConfigIncorrectException("verify code mismatch!");
                    }
                }
                Tuple<List<string>, List<string>> tuple = sysResourceService.GetRolesAndPermissions(user.Id);
                long? tenantId = null;
                if (StringUtils.GetEnumValue(WebConstants.ACCOUNT_TYPE.SYSADMIN).Equals(user.AccountType))
                {
                    tenantId = 0;
                }
                else
                {
                    ITenantInfoService tenantInfoService = RegServiceContext.GetBean<ITenantInfoService>();
                    List<Dictionary<string, object>> tenants = tenantInfoService.GetUserTenants(user.Id);
                    if (tenants.IsNullOrEmpty())
                    {
                        throw new ConfigMissingException("user does't belong to any tenant!");
                    }
                    if (tenants.Count > 1)
                    {
                        ISysCodesetService sysCodesetService = RegServiceContext.GetBean<ISysCodesetService>();
                        sysCodesetService.FilterListNewByCodeSet<Dictionary<string, object>>(tenants, "type", "typeName", "TENANTUSERTYPE");
                        retDict.TryAdd("tenants", tenants);
                        retDict.TryAdd("selectTenant", true);
                    }
                    else
                    {
                        tenantId = Convert.ToInt64(tenants[0]["id"]);
                    }
                }
                //新初始化用户，首次登录需修改密码
                if (Convert.ToInt32(WebConstants.AUDIT_TYPE.INIT).Equals(user.UserStatus))
                {
                    retDict.TryAdd("needInitPwd", true);
                }

                var sessionId = Guid.NewGuid().ToString("N");
                List<Claim> claims =
                [
                    new Claim(ClaimTypes.NameIdentifier, Convert.ToString(user.Id)),
                    new Claim(ClaimTypes.Name, user.UserAccount),
                    new Claim(JwtRegisteredClaimNames.Jti, sessionId)
                ];
                if (!tuple.Item1.IsNullOrEmpty())
                {
                    foreach (string roleStr in tuple.Item1)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roleStr));
                    }
                }
                if (!tuple.Item2.IsNullOrEmpty())
                {
                    claims.Add(new Claim("PermissionCodes", string.Join(',', tuple.Item2)));
                }
                if (tenantId != null)
                {
                    string? tenantIdStr = tenantId.ToString();
                    Trace.Assert(!string.IsNullOrWhiteSpace(tenantIdStr), "");
                    claims.Add(new Claim("tenantId", tenantIdStr));
                }
                //生成JWT
                DateTime expiration;
                GetToken(jwtSecurity, claims, out expiration, out string tokenString);
                string redisKey = $"user:jwt:{sessionId}";
                await _redisDb.StringSetAsync(redisKey, tokenString, TimeSpan.FromDays(2));
                // 4. 定义行业标准安全系数最高的 Cookie 配置
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    //SameSite = SameSiteMode.Lax,
                    // Domain = ".yourcompany.com", 
                    Expires = expiration
                };
                // 下发 Cookie A：存储高内聚完整的加密 JWT 字节流
                //context.Response.Cookies.Append("AuthToken", tokenString, cookieOptions);
                //context.Response.Cookies.Append("sessionKey", sessionId, cookieOptions);

                retDict.TryAdd("sessionKey", sessionId);
                retDict.TryAdd("token", tokenString);

                return retDict;
            }
            catch (Exception ex)
            {
                throw new NotSupportedException(ex.Message);
            }
        }
        public void SwitchTenant(HttpContext context, byte[] jwtSecurity, IDatabase _redisDb, long tenantId, List<string> permissions, string sessionId, out string tokenString)
        {
            List<Claim> tenantIdClaims = context.User.Claims.Where(x => !x.Type.Equals("tenantId") && !x.Type.Equals("PermissionCodes")).ToList();
            tenantIdClaims.Add(new Claim("PermissionCodes", string.Join(',', permissions)));
            tenantIdClaims.Add(new Claim("tenantId", tenantId.ToString()));
            GetToken(jwtSecurity, tenantIdClaims, out DateTime expiration, out tokenString);
            string redisKey = $"user:jwt:{sessionId}";
            _redisDb.StringSet(redisKey, tokenString, TimeSpan.FromDays(2));
        }

        private static void GetToken(byte[] jwtSecurity, List<Claim> claims, out DateTime expiration, out string tokenString)
        {
            var key = new SymmetricSecurityKey(jwtSecurity);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            expiration = DateTime.UtcNow.AddDays(2);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );
            tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
