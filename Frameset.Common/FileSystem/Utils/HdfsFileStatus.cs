namespace Frameset.Common.FileSystem.Utils
{
    public class HdfsFileStatus
    {
        public bool IsDirectory
        {
            get; set;
        } = false;
        public long Size
        {
            get; set;
        }
        public string Path
        {
            get; set;
        } = string.Empty;
        public long LastAccess
        {
            get; set;
        }

    }
}
