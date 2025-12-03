using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model
{
    [MappingEntity("t_sys_role_info")]
    public class SysRole : BaseEntity
    {
        [MappingField(IfPrimary = true, IfIncrement = true)]
        public long Id
        {
            get; set;
        }
        public string RoleName
        {
            get; set;
        } = string.Empty;
        public string RoleType
        {
            get; set;
        } = string.Empty;
        public string RoleCode
        {
            get; set;
        } = string.Empty;
    }
}
