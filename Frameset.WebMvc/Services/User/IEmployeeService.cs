using Frameset.Core.Service;
using Frameset.Web.Model.User;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.QueryDto;

namespace Frameset.WebMvc.Services.User
{
    public interface IEmployeeService : IService<Employee, long>
    {
        List<Employee> Search(EmployeeQueryDto dto);
        bool SaveEmployee(EmployeeDto dto);
    }
}
