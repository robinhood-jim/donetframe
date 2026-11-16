using Frameset.Web.Annotation;
using Frameset.Web.Controller;
using Frameset.Web.Filter;
using Frameset.Web.Model.User;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frameset.WebMvc.Controllers.User
{
    [ApiController]
    [Route("system/employee/")]
    public class EmployeeController : AbstractController<IEmployeeService, Employee, long>
    {
        [AllowAnonymous]
        public IActionResult Register([FromBody] EmployeeDto dto)
        {
            return null;
        }
        [AllowAnonymous]
        [HttpGet("checkPhone/{phoneNum}")]
        [RepeatSubmit]
        [TypeFilter(typeof(RepeatSubmitFilter))]
        public IActionResult CheckPhoneNum(string phoneNum)
        {
            return null;
        }

    }
}
