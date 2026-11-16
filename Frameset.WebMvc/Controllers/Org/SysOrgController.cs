using Frameset.Core.Context;
using Frameset.Web.Common;
using Frameset.Web.Controller;
using Frameset.Web.Model.Org;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.QueryDto;
using Frameset.WebMvc.Services.Org;
using Frameset.WebMvc.Vo;
using Microsoft.AspNetCore.Mvc;

namespace Frameset.WebMvc.Controllers.Org
{
    [Route("system/org/")]
    public class SysOrgController : AbstractController<ISysOrgService, SysOrg, long>
    {
        public SysOrgController() : base()
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
        [HttpGet("listUser")]
        public IActionResult QueryOrgUser([FromQuery] SysOrgUserDto userDto)
        {
            CustomDateTimeConverter convert = RegServiceContext.GetBean<CustomDateTimeConverter>();
            convert.SetDateTimeFormat("yyyy-MM-dd");
            var queryRs = _service.QueryOrgUsers(userDto);
            return OutputMsg(queryRs);
        }
        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            return RemoveEntity(id);
        }
        [HttpGet("list")]
        public IActionResult Query([FromQuery] SysOrgDto dto)
        {
            List<SysOrgVO> sysOrgs = _service.QueryOrg(dto);
            return OutputMsg(sysOrgs);
        }
        [HttpPost("join")]
        public IActionResult JoinOrg([FromBody] Dictionary<string, string> reqMap)
        {
            if (reqMap.TryGetValue("orgId", out string? orgIdStr) && reqMap.TryGetValue("userIds", out string? userIdsStr))
            {
                long orgId = Convert.ToInt64(orgIdStr);
                List<long> userIds = userIdsStr.Split(',').Select(x => Convert.ToInt64(x)).ToList();
                if (_service.JoinOrg(HttpContext, orgId, userIds) > 0)
                {
                    return OutputMsg("邀请加入组织成功,等待用户确认邀请");
                }
                else
                {
                    return OutputErrMsg("保存失败");
                }
            }
            else
            {
                return OutputErrMsg("require parameter orgId and userIds");
            }
        }



    }
}
