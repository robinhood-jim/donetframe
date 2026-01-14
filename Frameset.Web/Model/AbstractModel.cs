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
        public string Status
        {
            get; set;
        } = string.Empty;
        public long? TenantId
        {
            get; set;
        }
    }
}
