using Frameset.Core.Annotation;

namespace Frameset.Web.Model.User
{
    [MappingEntity("t_sys_employer")]
    public class Employer : AbstractModel
    {
        [MappingField(IfPrimary = true, IfIncrement = true)]
        public long Id
        {
            get; set;
        }
        public string Name
        {
            get; set;
        } = string.Empty;

        public DateTime? BirthDay
        {
            get; set;
        }
        public string CreditNo
        {
            get; set;
        } = string.Empty;

        public string PhoneNum
        {
            get; set;
        } = string.Empty;
        public int Gender
        {
            get; set;
        }
        public string District
        {
            get; set;
        } = string.Empty;
        public string Address
        {
            get; set;
        } = string.Empty;
        public int AuditStatus
        {
            get; set;
        }

    }
}
