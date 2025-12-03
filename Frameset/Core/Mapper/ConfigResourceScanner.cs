using System.IO;

namespace Frameset.Core.Mapper
{
    public static class ConfigResourceScanner
    {
        public static FileInfo[] DoScan(string configPath)
        {
            string currPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string listPath = currPath + configPath;
            if (Directory.Exists(listPath))
            {
                DirectoryInfo info = new DirectoryInfo(listPath);
                return info.GetFiles("*.xml", SearchOption.AllDirectories);

            }
            else
            {
                throw new FileNotFoundException(configPath + " not found");
            }
        }
    }
}
