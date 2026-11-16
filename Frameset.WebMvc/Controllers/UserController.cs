using Frameset.Core.Repo;
using Frameset.Web.Controller;
using Frameset.Web.Model.User;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Frameset.WebMvc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : AbstractControllerBase
    {
        private readonly IBaseRepository<SysUser, long> userRepository;
        public UserController(IBaseRepository<SysUser, long> repository)
        {
            userRepository = repository;
        }
        [Route("getUser")]
        [HttpGet]
        public IActionResult GetCurrrentUser()
        {
            long userId = GetUserIdFromToken();
            return new JsonResult(userRepository.GetById(userId));

        }
        protected long GetUserIdFromToken()
        {
            long userId = 0;
            try
            {
                Trace.Assert(HttpContext.User.Identity != null);
                if (HttpContext.User.Identity.IsAuthenticated)
                {
                    var identity = HttpContext.User.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        string? userIdStr = identity?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                        long.TryParse(userIdStr, out userId);
                    }
                }
                return userId;
            }
            catch
            {
                return 0;
            }
        }

    }
}
