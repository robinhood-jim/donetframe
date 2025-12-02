using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model
{
    [MappingEntity("t_sys_user_info")]
    public class SysUser : BaseEntity
    {
        [MappingField(IfPrimary = true, IfIncrement = true)]
        public long Id
        {
            get; set;
        }
        private long OrgId
        {
            get; set;
        }
        public string UserName
        {
            get; set;
        } = string.Empty;
        public string UserAccount
        {
            get; set;
        } = string.Empty;
        public string AccountType
        {
            get; set;
        } = "1";
        public string Remark
        {
            get; set;
        } = string.Empty;

    }
}
