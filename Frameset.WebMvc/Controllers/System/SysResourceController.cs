using Frameset.Web.Controller;
using Frameset.Web.Model.System;
using Frameset.Web.Utils;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.Services.System;
using Microsoft.AspNetCore.Mvc;

namespace Frameset.WebMvc.Controllers.System
{
    [ApiController]
    [Route("system/menu")]
    public class SysResourceController : AbstractController<ISysResourceService, SysResource, long>
    {
        ISecurityUtils securityUtils;
        public SysResourceController(ISysResourceService baseService, ISecurityUtils securityUtils) : base(baseService)
        {
            this.securityUtils = securityUtils;
        }
        [HttpPost("show")]
        public IActionResult Search([FromBody] Dictionary<string, object>? dict)
        {
            List<SysResourceDto> dtos = _service.Search(dict);
            return OutputMsg(dtos);
        }
        [HttpGet("getByUser/{userId}")]
        public IActionResult GetPermissionByUser(long userId)
        {
            Dictionary<string, object> ret = new();
            List<long> list = _service.GetPermissions(userId, securityUtils.GetLoginTenant());
            ret.TryAdd("ids", list);
            return OutputMsg(ret);
        }
    }
}
