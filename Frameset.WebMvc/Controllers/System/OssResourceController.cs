using Frameset.Common.FileSystem;
using Frameset.Core.Configuration;
using Frameset.Core.Context;
using Frameset.Core.Utils;
using Frameset.Web.Controller;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Frameset.WebMvc.Controllers.System
{
    [ApiController]
    [Route("ossres/")]
    public class OssResourceController : AbstractControllerBase
    {
        private readonly IFileSystem fileSystem;
        public OssResourceController(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        [HttpGet]
        public ActionResult ShowResource([FromQuery] string path)
        {
            string resPath = path;
            ProjectConfiguration configuration = RegServiceContext.GetBean<ProjectConfiguration>();
            Dictionary<object, object>? fileSystemConfig = configuration.GetConfig<Dictionary<object, object>>("fileSystem");
            Trace.Assert(fileSystemConfig != null);
            fileSystemConfig.TryGetValue("startPath", out object? basePath);
            if (basePath != null && !string.IsNullOrWhiteSpace(basePath.ToString()))
            {
                resPath = basePath.ToString() + resPath;
            }
            byte[] output = ReturnOssResource(fileSystem, resPath);
            FileMeta meta = FileUtil.Parse(resPath);
            return File(output, meta.ContentType);
        }
        private byte[] ReturnOssResource(IFileSystem fileSystem, string ossPath)
        {
            using Stream inputStream = fileSystem.GetRawInputStream(ossPath);
            using MemoryStream outputStream = new MemoryStream();
            inputStream.CopyTo(outputStream);
            return outputStream.ToArray();

        }

    }
}
