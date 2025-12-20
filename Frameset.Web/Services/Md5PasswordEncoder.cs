using System.Security.Cryptography;
using System.Text;

namespace Frameset.Web.Services
{
    public sealed class Md5PasswordEncoder : IPasswordEncoder
    {
        public string EncodeOrigin(string originPasswd)
        {
            return ComputeMd5(originPasswd);
        }

        public bool Match(string encryptStr, string origin)
        {
            return (string.Equals(ComputeMd5(origin), encryptStr, StringComparison.OrdinalIgnoreCase));
        }
        private string ComputeMd5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in data)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
