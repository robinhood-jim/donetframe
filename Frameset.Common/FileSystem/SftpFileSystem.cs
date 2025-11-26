using Frameset.Common.Data;
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
        internal string userName;
        internal string host = ResourceConstants.DEFAULTHOST;
        internal string password;
        int port = ResourceConstants.SFTPDEFAULTPORT;
        public SftpFileSystem(DataCollectionDefine define) : base(define)
        {
            identifier = Constants.FileSystemType.SFTP;
            Init(define);
        }
        public override void Init(DataCollectionDefine define)
        {
            base.Init(define);
            if (define.ResourceConfig.Count > 0)
            {
                string portStr;
                string hostStr;
                if (define.ResourceConfig.TryGetValue(ResourceConstants.SFTPHOST, out hostStr))
                {
                    host = hostStr ?? ResourceConstants.DEFAULTHOST;
                }

                if (define.ResourceConfig.TryGetValue(ResourceConstants.SFTPPORT, out portStr))
                {
                    port = Convert.ToInt32(portStr);
                }
                define.ResourceConfig.TryGetValue(ResourceConstants.SFTPUSERNAME, out userName);
                define.ResourceConfig.TryGetValue(ResourceConstants.SFTPPASSWD, out password);

                client = new SftpClient(host, port, userName, password);

                busyTag = false;
            }
            else
            {
                throw new NotSupportedException("must config paramter sftp ");
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

        public override bool Exist(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                return client.Exists(resourcePath);
            }
            finally
            {
                FinishOperator();
            }
        }



        public override Stream GetInputStream(string resourcePath)
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
            }
            catch (Exception ex)
            {
                FinishOperator();
                throw new OperationFailedException(ex.Message, ex);
            }
            return null;
        }

        public override Stream GetOutputStream(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                if (!client.Exists(resourcePath))
                {
                    return GetOutputStremWithCompress(resourcePath, client.OpenWrite(resourcePath));
                }
                else
                {
                    throw new OperationNotAllowedException("path already Exists!");
                }
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
                if (client.Exists(resourcePath))
                {
                    ISftpFile file = client.Get(resourcePath);
                    if (file.IsRegularFile)
                    {
                        return new BufferedStream(client.OpenRead(resourcePath));
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
            }
            catch (Exception ex)
            {
                FinishOperator();
                throw new OperationFailedException(ex.Message, ex);
            }
            return null;
        }

        public override Stream GetRawOutputStream(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                if (!client.Exists(resourcePath))
                {
                    return new BufferedStream(client.OpenWrite(resourcePath));
                }
                else
                {
                    throw new OperationNotAllowedException("path already Exists!");
                }
            }
            catch (Exception ex)
            {
                FinishOperator();
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
                FinishOperator();
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
        public override void FinishWrite(Stream outputStream)
        {
            outputStream.Flush();
            outputStream.Close();
            FinishOperator();
        }
    }
}
