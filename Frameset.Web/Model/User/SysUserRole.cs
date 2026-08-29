using Frameset.Core.Annotation;

namespace Frameset.Web.Model.User
{
    public class SysUserRole : AbstractModel
    {
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
        }
        public string Oper
        {
            get; set;
        }
    }
}
