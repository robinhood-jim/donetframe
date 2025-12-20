namespace Frameset.Web.Services
{
    public sealed class BCryptPasswordEncoder : IPasswordEncoder
    {
        public string EncodeOrigin(string originPasswd)
        {
            return BCrypt.Net.BCrypt.HashPassword(originPasswd);
        }

        public bool Match(string encryptStr, string origin)
        {
            return BCrypt.Net.BCrypt.Verify(origin, encryptStr);
        }
    }
}
