using Frameset.Core.Annotation;

namespace Frameset.Web.Model.Org
{
    [MappingEntity("t_tenant_user_r")]
    public class TenantUser : AbstractModel
    {
        [MappingField(IfIncrement = true, IfPrimary = true)]
        public long Id
        {
            get; set;
        }
        public long UserId
        {
            get; set;
        }

        public int Type
        {
            get; set;
        }
    }
}
