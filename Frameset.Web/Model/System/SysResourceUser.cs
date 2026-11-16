using Frameset.Core.Annotation;

namespace Frameset.Web.Model.System
{
    [MappingEntity("t_sys_resource_user_r")]
    public class SysResourceUser : AbstractModel
    {
        [MappingField(IfIncrement = true, IfPrimary = true)]
        public long Id
        {
            get; set;
        }
        public long ResId
        {
            get; set;
        }
        public long UserId
        {
            get; set;
        }
        public int AssingType
        {
            get; set;
        }


    }
}
