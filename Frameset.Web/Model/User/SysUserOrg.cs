using Frameset.Core.Annotation;

namespace Frameset.Web.Model.User
{
    [MappingEntity("t_sys_user_org_r")]
    public class SysUserOrg : AbstractModel
    {
        [MappingField(IfPrimary = true, IfIncrement = true)]
        public long Id
        {
            get; set;
        }
        public long OrgId
        {
            get; set;
        }
        public long UserId
        {
            get; set;
        }

    }
}
