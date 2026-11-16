using Frameset.Core.Common;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Web.Controller;
using Frameset.Web.Model.User;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace Frameset.WebMvc.Controllers.User
{
    [ApiController]
    [Route("system/role/")]
    public class SysRoleController : AbstractController<ISysRoleService, SysRole, long>
    {
        public SysRoleController(ISysRoleService baseService) : base(baseService)
        {
        }

        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            return GetEntity(id);
        }
        [HttpPost]
        public IActionResult Save([FromBody] Dictionary<string, object> dict)
        {
            return SaveEntity(dict);
        }
        [HttpPut]
        public IActionResult Update([FromBody] Dictionary<string, object> dict)
        {
            return UpdateEntity(dict);
        }
        [Route("{action}")]
        [HttpPost]
        public IActionResult Search([FromBody] PageQuery query)
        {
            PageDTO<SysRole> pageDTO = _service.QueryModelsPage(query);
            return OutputMsg(pageDTO);
        }
        [HttpDelete("{id}")]
        public IActionResult Remove(long id)
        {
            return RemoveEntity(id);
        }
        [HttpGet("all/")]
        public IActionResult GetAllRoles()
        {
            List<SysRole> roles = _service.QueryModeslByField(nameof(SysRole.Status), Constants.SqlOperator.EQ, [Constants.VALID]);
            return OutputMsg(roles.Select(r => RoleDto.FromVO(r)));
        }
        [HttpGet("menu/{roleId}")]
        public IActionResult ShowRoleMenu(long roleId)
        {
            return null;
        }

    }
}
