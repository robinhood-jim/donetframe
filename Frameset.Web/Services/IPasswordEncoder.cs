namespace Frameset.Web.Services
{
    public interface IPasswordEncoder
    {
        string EncodeOrigin(string originPasswd);
        bool Match(string encryptStr, string origin);
    }
}
