using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model.User
{
    [MappingEntity("t_user_system_notice")]
    public class UserSystemNotice : BaseEntity
    {
        [MappingField(IfIncrement = true, IfPrimary = true)]
        public long Id
        {
            get; set;
        }
        public long SystemNoticeId
        {
            get; set;
        }
        public long RecipientId
        {
            get; set;
        }
        public int State
        {
            get; set;
        }
        public DateTime PullTime
        {
            get; set;
        }
    }
}
