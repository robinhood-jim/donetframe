using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model.Org
{
    [MappingEntity("t_employee")]
    public class Employee : BaseEntity
    {
        [MappingFieldAttribute(ifPrimary: true, ifIncrement: true)]
        public long Id
        {
            get; set;
        }
        public string Name
        {
            get; set;
        } = string.Empty;
        public DateTime? BrithDay
        {
            get; set;
        }
        public string CreditNo
        {
            get; set;
        } = string.Empty;
        public string ContactPhone
        {
            get; set;
        } = string.Empty;
        public short? Gender
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


    }
}
