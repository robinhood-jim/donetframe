using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Renci.SshNet;
using Renci.SshNet.Sftp;

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
            identifier = Constants.FileSystemType.SFTP;
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

                client = new SftpClient(host, port, userName, password);

                busyTag = false;
            }
            else
            {
                throw new NotSupportedException("must config paramter ftp ");
            }
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override bool Exist(string resourcePath)
        {
            BeginOperator();
            try
            {
                return client.Exists(resourcePath);
            }
            finally
            {
                FinishOperator();
            }
        }



        public override Stream? GetInputStream(string resourcePath)
        {
            BeginOperator();
            if (client.Exists(resourcePath))
            {
                ISftpFile file = client.Get(resourcePath);

                if (file.IsRegularFile)
                {
                    return GetInputStreamWithCompress(resourcePath, client.OpenRead(resourcePath));
                }
                else if (file.IsDirectory)
                {
                    throw new NotSupportedException("source is Path,can not open!");
                }
            }
            else
            {
                throw new NotSupportedException("source is Path,can not open!");
            }
            return null;
        }

        public override Stream? GetOutputStream(string resourcePath)
        {
            BeginOperator();
            if (!client.Exists(resourcePath))
            {
                return GetOutputStremWithCompress(resourcePath, client.OpenWrite(resourcePath));
            }
            else
            {
                throw new OperationNotAllowedException("path already Exists!");
            }
        }

        public override Stream? GetRawInputStream(string resourcePath)
        {
            if (client.Exists(resourcePath))
            {
                ISftpFile file = client.Get(resourcePath);
                if (file.IsRegularFile)
                {
                    return client.OpenRead(resourcePath);
                }
                else if (file.IsDirectory)
                {
                    throw new NotSupportedException("source is Path,can not open!");
                }
            }
            else
            {
                throw new OperationNotAllowedException("path not Exists!");
            }
            return null;
        }

        public override Stream? GetRawOutputStream(string resourcePath)
        {
            BeginOperator();
            if (!client.Exists(resourcePath))
            {
                return client.OpenWrite(resourcePath);
            }
            else
            {
                throw new OperationNotAllowedException("path already Exists!");
            }
        }

        public override Tuple<Stream, StreamReader>? GetReader(string resourcePath)
        {
            Stream? input = GetInputStream(resourcePath);
            if (input != null)
            {
                return Tuple.Create(input, new StreamReader(input));
            }
            else
            {
                return null;
            }
        }

        public override long GetStreamSize(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                if (client.Exists(resourcePath))
                {
                    ISftpFile file = client.Get(resourcePath);
                    if (file.IsRegularFile)
                    {
                        return file.Length;
                    }
                    else
                    {
                        throw new OperationNotAllowedException("path " + resourcePath + "is not a valid file");
                    }

                }
                else
                {
                    return -1;
                }
            }
            finally
            {
                FinishOperator();
            }
        }

        public override Tuple<Stream, StreamWriter>? GetWriter(string resourcePath)
        {
            Stream? input = GetOutputStream(resourcePath);
            if (input != null)
            {
                return Tuple.Create(input, new StreamWriter(input));
            }
            else
            {
                return null;
            }
        }

        public override bool IsDirectory(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                if (client.Exists(resourcePath))
                {
                    ISftpFile file = client.Get(resourcePath);
                    return file.IsDirectory;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message, ex);
            }
        }
        public override void FinishOperator()
        {
            if (Interlocked.Read(ref count) == 0)
            {
                if (client != null)
                {
                    client.Disconnect();
                }
                Interlocked.Increment(ref count);
            }
        }
    }
}
