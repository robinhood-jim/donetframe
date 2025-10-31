using Frameset.Core.FileSystem;
using Renci.SshNet;

namespace Frameset.Common.FileSystem
{
    public class SftpFileSystem : AbstractFileSystem
    {
        private SftpClient client;
        internal string ftpUri;
        internal string userName = null;
        internal string host = "localhost";
        internal string password = null;
        int port = 22;
        public SftpFileSystem(DataCollectionDefine define) : base(define)
        {

        }
        public override void Init(DataCollectionDefine define)
        {
            base.Init(define);
            if (define.ResourceConfig.Count > 0)
            {
                string portStr = null;

                define.ResourceConfig.TryGetValue("ftp.host", out host);
                if (define.ResourceConfig.TryGetValue("ftp.port", out portStr))
                {
                    port = Convert.ToInt32(portStr);
                }
                define.ResourceConfig.TryGetValue("ftp.userName", out userName);
                define.ResourceConfig.TryGetValue("ftp.password", out password);

                client = new SftpClient(host,port, userName, password);
                busyTag = false;
            }
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override bool Exist(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override void FinishWrite(Stream outStream)
        {
            throw new NotImplementedException();
        }

        public override Stream? GetInputStream(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override Stream? GetOutputStream(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override Stream? GetRawInputStream(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override Stream? GetRawOutputStream(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override Tuple<Stream, StreamReader>? GetReader(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override long GetStreamSize(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override Tuple<Stream, StreamWriter>? GetWriter(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override bool IsDirectory(string resourcePath)
        {
            throw new NotImplementedException();
        }
    }
}
