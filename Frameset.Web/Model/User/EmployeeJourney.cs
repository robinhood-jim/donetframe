using Frameset.Core.Annotation;

namespace Frameset.Web.Model.User
{
    [MappingEntity("t_sys_employee_journey")]
    public class EmployeeJourney
    {
        public long Id
        {
            get; set;
        }
        public long EmpId
        {
            get; set;
        }
        public long EntId
        {
            get; set;
        }
        public DateTime ActivateTime
        {
            get; set;
        }
        public DateTime RetireTime
        {
            get; set;
        }
        public string Type
        {
            get; set;
        } = string.Empty;

    }
}
