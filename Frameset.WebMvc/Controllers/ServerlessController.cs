using Frameset.Common.FileSystem;
using Frameset.Web.Controller;
using Frameset.Web.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Frameset.WebMvc.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ServerlessController : AbstractControllerBase
    {
        [HttpGet("{action}")]
        public IActionResult Register(string path)
        {
            IFileSystem? fileSystem = ApplicationContext.GetBean(typeof(IFileSystem)) as IFileSystem;
            fileSystem = fileSystem ?? LocalFileSystem.GetInstance();
            using (Stream fileStream = fileSystem.GetRawInputStream(path))
            {
                fileStream.RegisterFunction(path);
                return OutputMsg("OK");
            }
        }
        [HttpGet("{action}")]
        public IActionResult Remove(string path)
        {
            DynamicFunctionLoader.UnRegisterFunction(path);
            return OutputMsg("OK");
        }

    }
}
