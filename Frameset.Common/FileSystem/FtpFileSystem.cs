using System.Diagnostics;
using FluentFTP;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;

namespace Frameset.Common.FileSystem
{
    public class FtpFileSystem : AbstractFileSystem
    {

        internal string ftpUri = null!;
        internal string? userName;
        internal string host = ResourceConstants.DEFAULTHOST;
        internal string? password;
        int port = ResourceConstants.FTPDEFAULTPORT;
        FtpClient client = null!;
        public FtpFileSystem(DataCollectionDefine define) : base(define)
        {
            identifier = Constants.FileSystemType.FTP;
            Init(define);
        }
        public sealed override void Init(DataCollectionDefine define)
        {
            base.Init(define);


            if (define.ResourceConfig.Count > 0)
            {

                if (define.ResourceConfig.TryGetValue(ResourceConstants.FTPHOST, out string? hostStr))
                {
                    host = hostStr ?? ResourceConstants.DEFAULTHOST;
                }
                if (define.ResourceConfig.TryGetValue(ResourceConstants.FTPPORT, out string? portStr))
                {
                    port = Convert.ToInt32(portStr);
                }
                define.ResourceConfig.TryGetValue(ResourceConstants.FTPUSERNAME, out userName);
                define.ResourceConfig.TryGetValue(ResourceConstants.FTPPASSWD, out password);

                client = new FtpClient(host, userName, password, port);
                Trace.Assert(client!=null,"");
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
                    throw new FileNotFoundException(resourcePath);
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
                    throw new FileNotFoundException(resourcePath);
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

        public override Tuple<Stream, StreamReader> GetReader(string resourcePath)
        {
            Stream input = GetInputStream(resourcePath);
            if (input != null)
            {
                return Tuple.Create(input, new StreamReader(input));
            }
            else
            {
                throw new OperationFailedException("getreader " + resourcePath + " failed!");
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

        public override List<string> List(string resourcePath)
        {
            BeginOperator();
            bool isDir = false;
            try
            {
                client.Connect();
                if (client.DirectoryExists(resourcePath))
                {
                    isDir = true;
                }
                else if (!client.FileExists(resourcePath))
                {
                    return [];
                }
                if (isDir)
                {
                    string[] names = client.GetNameListing(resourcePath);
                    if (names != null && names.Length > 0)
                    {
                        return new(names);
                    }
                }

            }
            catch (Exception ex)
            {
                FinishOperator();
                throw new OperationFailedException(ex.Message, ex);
            }
            return [];
        }
        public override bool Delete(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                if (client.DirectoryExists(resourcePath))
                {
                    string[] names = client.GetNameListing(resourcePath);
                    if (names == null || names.Length == 0)
                    {
                        client.DeleteDirectory(resourcePath);
                    }
                }
                else if (client.FileExists(resourcePath))
                {
                    client.DeleteFile(resourcePath);
                }
                return true;

            }
            catch (Exception ex)
            {
                FinishOperator();
                throw new OperationFailedException(ex.Message, ex);
            }
        }

        public override bool CreateFile(string resourcePath)
        {
            BeginOperator();
            try
            {
                client.Connect();
                if (!client.DirectoryExists(resourcePath) && !client.FileExists(resourcePath))
                {
                    var stream = client.OpenWrite(resourcePath, FtpDataType.Binary, 0);
                    stream.Dispose();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message, ex);
            }
            finally
            {
                FinishOperator();
            }
        }

        protected override void Dispose(bool disposable)
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
