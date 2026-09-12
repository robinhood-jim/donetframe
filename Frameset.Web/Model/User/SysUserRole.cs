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
        [OneToMany(typeof(SysUser), "UserId")]
        public IList<SysUser> SysUsers
        {
            get; set;
        } = [];
        public string Oper
        {
            get; set;
        } = string.Empty;
    }
}
