using Frameset.Core.Annotation;

namespace Frameset.Web.Model.User
{
    [MappingEntity("t_sys_org_employee_r")]
    public class SysOrgEmployee : AbstractModel
    {
        [MappingField(IfIncrement = true, IfPrimary = true)]
        public long Id
        {
            get; set;
        }
        public long EmpId
        {
            get; set;
        }
        public long OrgId
        {
            get; set;
        }
        public long TtenantId
        {
            get; set;
        }
    }
}
