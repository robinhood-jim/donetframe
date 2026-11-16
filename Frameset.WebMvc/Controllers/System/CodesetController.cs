using Frameset.Web.Controller;
using Frameset.Web.Dto;
using Frameset.Web.Model.System;
using Frameset.WebMvc.Services.System;
using Microsoft.AspNetCore.Mvc;

namespace Frameset.WebMvc.Controllers.System
{
    [ApiController]
    [Route("system/code/")]
    public class CodesetController : AbstractController<ISysCodesetService, SysCodeset, long>
    {
        public CodesetController(ISysCodesetService baseService) : base(baseService)
        {
        }
        [HttpGet("{dictData}")]
        public ActionResult QueryDict(string dictData)
        {
            List<SysCode>? codes = _service.GetSysCode(dictData);
            return OutputMsg(codes.Select(c => SysCodeDto.FromModel(c)));
        }
    }
}
