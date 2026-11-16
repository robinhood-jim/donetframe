namespace Frameset.WebMvc.Dto
{
    public class SysRoleDto
    {
        public long Id
        {
            get; set;
        }
        public string RoleName
        {
            get; set;
        } = string.Empty;
        public string RoleCode
        {
            get; set;
        } = string.Empty;
        public string RoleType
        {
            get; set;
        } = string.Empty;
        public List<long> Permissions
        {
            get; set;
        } = [];
    }
}
