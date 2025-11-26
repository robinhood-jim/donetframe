using FluentFTP;
using Frameset.Common.Data;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;

namespace Frameset.Common.FileSystem
{
    public class FtpFileSystem : AbstractFileSystem
    {

        internal string ftpUri;
        internal string userName;
        internal string host = ResourceConstants.DEFAULTHOST;
        internal string password;
        int port = ResourceConstants.FTPDEFAULTPORT;
        FtpClient client;
        public FtpFileSystem(DataCollectionDefine define) : base(define)
        {
            identifier = Constants.FileSystemType.FTP;
            Init(define);
        }
        public override void Init(DataCollectionDefine define)
        {
            base.Init(define);


            if (define.ResourceConfig.Count > 0)
            {
                string portStr;
                string hostStr;
                ;
                if (define.ResourceConfig.TryGetValue(ResourceConstants.FTPHOST, out hostStr))
                {
                    host = hostStr ?? ResourceConstants.DEFAULTHOST;
                }
                if (define.ResourceConfig.TryGetValue(ResourceConstants.FTPPORT, out portStr))
                {
                    port = Convert.ToInt32(portStr);
                }
                define.ResourceConfig.TryGetValue(ResourceConstants.FTPUSERNAME, out userName);
                define.ResourceConfig.TryGetValue(ResourceConstants.FTPPASSWD, out password);

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

        public override Stream GetInputStream(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                if (!client.FileExists(resourcePath))
                {
                    return null;
                }
                return GetInputStreamWithCompress(resourcePath, client.OpenRead(resourcePath));
            }
            catch (Exception ex)
            {
                FinishOperator();
                throw new OperationFailedException(ex.Message, ex);
            }

        }

        public override Stream GetOutputStream(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                if (client.FileExists(resourcePath))
                {
                    throw new NotSupportedException("path already exists!");
                }
                return GetOutputStremWithCompress(resourcePath, client.OpenWrite(resourcePath));

            }
            catch (Exception ex)
            {
                FinishOperator();
                throw new OperationFailedException(ex.Message, ex);
            }

        }

        public override Stream GetRawInputStream(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                if (!client.FileExists(resourcePath))
                {
                    return null;
                }
                return new BufferedStream(client.OpenRead(resourcePath));
            }
            catch (Exception ex)
            {
                FinishOperator();
                throw new OperationFailedException(ex.Message, ex);
            }

        }

        public override Stream GetRawOutputStream(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                if (client.FileExists(resourcePath))
                {
                    throw new OperationNotAllowedException("resource Exists!");
                }
                return new BufferedStream(client.OpenWrite(resourcePath));
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


        public override void Dispose(bool disposeable)
        {
            if (client != null)
            {
                if (client.IsConnected)
                {
                    client.Disconnect();
                }
                client.Dispose();
            }
            GC.SuppressFinalize(this);
        }
        public override void FinishWrite(Stream outputStream)
        {
            if (outputStream.CanWrite)
            {
                outputStream.Flush();
                outputStream.Close();
            }
            FinishOperator();
        }
    }
}
