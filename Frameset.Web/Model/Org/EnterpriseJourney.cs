using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model.Org
{
    [MappingEntity("t_sys_enterprise_journey")]
    public class EnterpriseJourney : BaseModel
    {
        [MappingField(IfIncrement = true, IfPrimary = true)]
        public long Id
        {
            get; set;
        }
        public long EntId
        {
            get; set;
        }
        public DateTime StartTime
        {
            get; set;
        }
        public DateTime EndTime
        {
            get; set;
        }
        public string Content
        {
            get; set;
        } = string.Empty;
        public long OwnerId
        {
            get; set;
        }

    }
}
