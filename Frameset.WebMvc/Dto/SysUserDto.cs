namespace Frameset.WebMvc.Dto
{
    public class SysUserDto
    {
        public SysUserDto()
        {

        }
        public long Id
        {
            get; set;
        }
        public string UserAccount
        {
            get; set;
        } = string.Empty;
        public string UserName
        {
            get; set;
        } = string.Empty;
        public string NickName
        {
            get; set;
        } = string.Empty;
        public string Email
        {
            get; set;
        } = string.Empty;
        public string PhoneNum
        {
            get; set;
        } = string.Empty;
        public string Remark
        {
            get; set;
        } = string.Empty;
        public string AccountType
        {
            get; set;
        } = string.Empty;
        public bool Status
        {
            get; set;
        }
        public List<long> Roles
        {
            get; set;
        } = [];
    }
}
