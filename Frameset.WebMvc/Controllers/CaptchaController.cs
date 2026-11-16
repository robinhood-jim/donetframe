using Lazy.Captcha.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frameset.WebMvc.Controllers
{
    [Route("captchaImage")]
    [AllowAnonymous]
    public class CaptchaController : ControllerBase
    {
        private ICaptcha captcha;
        private Random random;
        public CaptchaController(ICaptcha captcha)
        {
            this.captcha = captcha;
            random = new Random(1231231);
        }
        [HttpGet]
        public ActionResult GetKaptcha()
        {
            int randomNum = random.Next() + 1;
            var info = captcha.Generate(Convert.ToString(randomNum), 30);
            return Ok(new
            {
                uuid = Convert.ToString(randomNum),
                img = Convert.ToBase64String(info.Bytes)
            });
        }
    }
}
