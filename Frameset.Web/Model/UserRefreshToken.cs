using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model
{
    [MappingEntity("t_user_refresh_token")]
    public class UserRefreshToken : BaseEntity
    {
        [MappingField(IfPrimary = true)]
        public string Uid
        {
            get; set;
        } = string.Empty;
        public long UserId
        {
            get; set;
        }
        public DateTime CreateTm
        {
            get; set;
        }
        public DateTime ExpireTime
        {
            get; set;
        }
        public int Status
        {
            get; set;
        }
    }
}
