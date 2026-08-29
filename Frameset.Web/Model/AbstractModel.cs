using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model
{
    public abstract class AbstractModel : BaseEntity
    {
        public long? Creator
        {
            get; set;
        }
        public DateTime? CreateTm
        {
            get; set;
        }
        public DateTime? ModifyTm
        {
            get; set;
        }
        public long? Modifier
        {
            get; set;
        }
        [LogicColumn]
        public string Status
        {
            get; set;
        } = "1";
        public long? TenantId
        {
            get; set;
        }
    }
}
