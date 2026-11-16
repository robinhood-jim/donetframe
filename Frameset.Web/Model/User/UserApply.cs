using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model.User
{
    [MappingEntity("t_user_apply")]
    public class UserApply : BaseEntity
    {
        public long Id
        {
            get; set;
        }
        public string PhoneNum
        {
            get; set;
        } = string.Empty;
        public string Name
        {
            get; set;
        } = string.Empty;
        public string CreditNo
        {
            get; set;
        } = string.Empty;

        public int ApplyType
        {
            get; set;
        }


    }
}
