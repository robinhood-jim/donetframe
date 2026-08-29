using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Model;

namespace Frametest.Models
{
    public abstract class AbstractModel : BaseEntity
    {
        public long Creator
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
        public long Modifier
        {
            get; set;
        }
        [LogicColumn]
        public string Status
        {
            get; set;
        } = Constants.VALID;
        public long TenantId
        {
            get; set;
        }
    }
}
