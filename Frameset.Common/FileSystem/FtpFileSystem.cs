using FluentFTP;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;

namespace Frameset.Common.FileSystem
{
    public class FtpFileSystem : AbstractFileSystem
    {

        internal string ftpUri;
        internal string userName = null;
        internal string host = "localhost";
        internal string password = null;
        int port = 21;
        FtpClient client;
        long count = 1;
        public FtpFileSystem(DataCollectionDefine define) : base(define)
        {
            identifier = Constants.FileSystemType.FTP;
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

                client = new FtpClient(host, userName, password, port);
                busyTag = false;
            }
        }
        public override bool Exist(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                return client.FileExists(resourcePath);
            }
            finally
            {
                FinishOperator();
            }

        }

        public override void FinishOperator()
        {
            if (Interlocked.Read(ref count) == 0)
            {
                client.Disconnect();
                Interlocked.Increment(ref count);
            }
        }

        public override Stream? GetInputStream(string resourcePath)
        {
            BeginOperator();
            if (!client.FileExists(resourcePath))
            {
                return null;
            }
            try
            {
                client.Connect();
                return GetInputStreamWithCompress(resourcePath, client.OpenRead(resourcePath));

            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message, ex);
            }

        }

        public override Stream? GetOutputStream(string resourcePath)
        {
            BeginOperator();
            if (client.FileExists(resourcePath))
            {
                throw new NotSupportedException("path already exists!");
            }
            try
            {
                client.Connect();
                return GetOutputStremWithCompress(resourcePath, client.OpenWrite(resourcePath));
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message, ex);
            }
        }

        public override Stream? GetRawInputStream(string resourcePath)
        {
            BeginOperator();
            if (!client.FileExists(resourcePath))
            {
                return null;
            }
            try
            {
                client.Connect();
                return client.OpenRead(resourcePath);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message, ex);
            }

        }

        public override Stream GetRawOutputStream(string resourcePath)
        {
            BeginOperator();
            if (client.FileExists(resourcePath))
            {
                throw new OperationNotAllowedException("resource Exists!");
            }
            try
            {
                client.Connect();
                return client.OpenWrite(resourcePath);
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message, ex);
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
                if (client.FileExists(resourcePath))
                {
                    client.GetFileSize(resourcePath);
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
            return -1;
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
                return client.DirectoryExists(resourcePath);
            }
            finally
            {
                FinishOperator();
            }
        }


        public override void Dispose()
        {
            if (client != null)
            {
                if (client.IsConnected)
                {
                    client.Disconnect();
                }
                client.Dispose();
            }

        }
    }
}
