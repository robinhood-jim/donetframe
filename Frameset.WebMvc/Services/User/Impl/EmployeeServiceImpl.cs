using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.Linq;
using Frameset.Core.Service;
using Frameset.Core.Utils;
using Frameset.Web.Model.User;
using Frameset.WebMvc.Dto;
using Frameset.WebMvc.QueryDto;

namespace Frameset.WebMvc.Services.User.Impl
{
    [Service]
    public class EmployeeServiceImpl : ServiceImpl<Employee, long>, IEmployeeService
    {
        public List<Employee> Search(EmployeeQueryDto dto)
        {
            IQueryable<Employee> query = Query();
            query = query.WhereIf(!string.IsNullOrWhiteSpace(dto.Phone), e => e.ContactPhone.Contains(dto.Phone))
                .WhereIf(!string.IsNullOrWhiteSpace(dto.District), e => e.District.Equals(dto.District))
                .OrGroupIf(!string.IsNullOrWhiteSpace(dto.Name), g => g.Like(true, e => e.Name, dto.Name).Like(true, e => e.Address, dto.Name))
                .Skip(Convert.ToInt32((dto.CurrentPage - 1) * dto.PageSize)).Take(Convert.ToInt32(dto.PageSize));
            List<Employee> employees = query.ToList();
            CustomQueryProvider? cquery = query.Provider as CustomQueryProvider;
            if (cquery != null)
            {
                dto.Total = cquery.QueryCount;
            }
            return employees;

        }
        public bool SaveEmployee(EmployeeDto dto)
        {
            int count = dbContext.Count<Employee>(e => e.CreditNo.Equals(dto.CreditNo) && e.Status.Equals(Constants.VALID));
            if (count > 0)
            {
                throw new DuplicatedException($"employee with credit {dto.CreditNo} already exists!");
            }
            Employee employee = new();
            BeanUtils.CopyProperties(dto, employee);
            return SaveEntity(employee);

        }
        public bool UpdateEmployee(EmployeeDto dto)
        {
            return false;
        }
    }
}
