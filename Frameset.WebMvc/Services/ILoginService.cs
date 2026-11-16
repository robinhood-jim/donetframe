using Frameset.Web.Services;
using Frameset.WebMvc.Dto;
using Lazy.Captcha.Core;
using StackExchange.Redis;

namespace Frameset.WebMvc.Services
{
    public interface ILoginService
    {
        Task<Dictionary<string, object>> DoLogin(HttpContext context, IPasswordEncoder passwordEncoder, LoginRequest request, byte[] jwtSecurity, IDatabase _redisDb, bool verifyCaptcha, ICaptcha? captcha);
        void SwitchTenant(HttpContext context, byte[] jwtSecurity, IDatabase _redisDb, long tenantId, List<string> permissions, string sessionId, out string tokenString);
    }
}
