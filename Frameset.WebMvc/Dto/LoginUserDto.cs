using Frameset.Web.Model.User;
using System.Diagnostics;

namespace Frameset.WebMvc.Dto
{
    public class LoginUserDto
    {
        public long Id
        {
            get; internal set;
        }
        public string UserName
        {
            get; internal set;
        } = string.Empty;
        public string NickName
        {
            get; internal set;
        } = string.Empty;
        public string Avatar
        {
            get; internal set;
        } = string.Empty;
        public string Email
        {
            get; internal set;
        } = string.Empty;
        public string Phone
        {
            get; internal set;
        } = string.Empty;
        public long TenantId
        {
            get; internal set;
        }
        public static LoginUserDto FromSysUser(SysUser sysUser)
        {
            Trace.Assert(sysUser != null, "");
            LoginUserDto dto = new();
            dto.Id = sysUser.Id;
            dto.UserName = sysUser.UserAccount;
            dto.NickName = sysUser.UserName;
            dto.Avatar = sysUser.Avatar;
            dto.Email = sysUser.Email;
            dto.Phone = sysUser.PhoneNum;
            return dto;
        }

    }
}
