using Frameset.Core.Common;
using Frameset.Core.Model;

namespace Frameset.Web.Model
{
    public abstract class AbstractModel : BaseEntity
    {
        public long Creator
        {
            get; set;
        }
        public DateTime CreateTm
        {
            get; set;
        }
        public long Modifier
        {
            get; set;
        }
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
