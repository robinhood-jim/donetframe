using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model.System
{
    [MappingEntity("t_system_notice")]
    public class SystemNotice : BaseEntity
    {
        [MappingField(IfPrimary = true, IfIncrement = true)]
        public long Id
        {
            get; set;
        }
        public long RecipientId
        {
            get; set;
        }
        public string Title
        {
            get; set;
        } = string.Empty;
        public long ManagerId
        {
            get; set;
        }
        public string Content
        {
            get; set;
        } = string.Empty;
        public DateTime PublishTime
        {
            get; set;
        }
        public int State
        {
            get; set;
        }

    }
}
