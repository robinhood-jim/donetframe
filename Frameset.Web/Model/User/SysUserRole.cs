using Frameset.Core.Annotation;

namespace Frameset.Web.Model.User
{
    [MappingEntity("t_sys_user_role_r")]
    public class SysUserRole : AbstractModel
    {
        [MappingField(IfIncrement = true, IfPrimary = true)]
        public long Id
        {
            get; set;
        }
        public long RoleId
        {
            get; set;
        }


        public long UserId
        {
            get; set;
        }
        [ManyToOne(typeof(SysUser), "UserId")]
        public SysUser SysUser
        {
            get; set;
        } = null!;
        public string Oper
        {
            get; set;
        } = string.Empty;
    }
}
