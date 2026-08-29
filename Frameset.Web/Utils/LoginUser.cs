namespace Frameset.Web.Utils
{
    public class LoginUser
    {
        internal LoginUser()
        {

        }
        public long UserId
        {
            get; internal set;
        }
        public string UserName
        {
            get; internal set;
        } = string.Empty;
        public string AccountName
        {
            get; internal set;
        } = string.Empty;
        public List<string> Permissions
        {
            get; internal set;
        } = [];
        public List<string> Roles
        {
            get; internal set;
        } = [];
        public long ExpireTime
        {
            get; internal set;
        }
        public string MobilePhone
        {
            get; internal set;
        } = string.Empty;
        public DateTime BirthDate
        {
            get; internal set;
        }
        public long TenantId
        {
            get; set;
        }
    }
    public class LoginUserBuilder
    {
        private LoginUser user;
        private LoginUserBuilder()
        {
            user = new LoginUser();
        }
        public static LoginUserBuilder NewBuilder()
        {
            return new LoginUserBuilder();
        }
        public LoginUserBuilder UserId(long userId)
        {
            user.UserId = userId;
            return this;
        }
        public LoginUserBuilder UserName(string userName)
        {
            user.UserName = userName;
            return this;
        }
        public LoginUserBuilder Permission(List<string> _permission)
        {
            user.Permissions = _permission;
            return this;
        }
        public LoginUserBuilder Roles(List<string> _roles)
        {
            user.Roles = _roles;
            return this;
        }
        public LoginUserBuilder MobilePhone(string phone)
        {
            user.MobilePhone = phone;
            return this;
        }
        public LoginUserBuilder BrithDate(DateTime birthDate)
        {
            user.BirthDate = birthDate;
            return this;
        }
        public LoginUserBuilder TenantId(long tenantId)
        {
            user.TenantId = tenantId;
            return this;
        }
        public LoginUser Build()
        {
            return user;
        }


    }
}
