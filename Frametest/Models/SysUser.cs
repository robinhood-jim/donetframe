using Frameset.Core.Annotation;

namespace Frametest.Models
{
    [MappingEntity("t_sys_user_info")]

    public class SysUser : AbstractModel
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
        public string UserPassword
        {
            get; set;
        } = string.Empty;
        public string PhoneNum
        {
            get; set;
        }
        public long EmployeeId
        {
            get; set;
        }

    }
}
