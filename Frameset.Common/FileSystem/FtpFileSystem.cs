using FluentFTP;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Serilog;
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
        Stream workingStream;
        ThreadLocal<FtpClient> clientLocal;
        public FtpFileSystem(DataCollectionDefine define) : base(define)
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
                client.Disconnect();
                busyTag = false;
            }

        }

        public override void FinishWrite(Stream outStream)
        {

        }
        public void FinishWrite(string path)
        {
            if (busyTag && workingStream != null)
            {
                try
                {
                    client.Connect();
                    FtpStatus status = client.UploadStream(workingStream, path);
                    if (status != FtpStatus.Success)
                    {
                        throw new OperationFailedException("");
                    }
                }
                finally
                {
                    client.Disconnect();
                }
                busyTag = false;
            }
        }
        public void FinishRead()
        {
            if (busyTag && workingStream != null)
            {
                client.Disconnect();
                busyTag = false;
            }
        }

        public override Stream? GetInputStream(string resourcePath)
        {
            BeginOperator();
            if (!Exist(resourcePath))
            {
                return null;
            }
            workingStream = new MemoryStream();
            try
            {
                client.Connect();
                if (client.DownloadStream(workingStream, resourcePath))
                {
                    return GetInputStreamWithCompress(resourcePath, workingStream);
                }
                else
                {
                    throw new UnauthorizedAccessException("");
                }
            }
            finally
            {
                FinishRead();
            }
        }

        public override Stream? GetOutputStream(string resourcePath)
        {
            BeginOperator();
            workingStream = new MemoryStream();
            return GetOutputStremWithCompress(resourcePath, workingStream);
        }

        public override Stream? GetRawInputStream(string resourcePath)
        {
            BeginOperator();
            if (!Exist(resourcePath))
            {
                return null;
            }
            workingStream = new MemoryStream();
            try
            {
                client.Connect();
                if (client.DownloadStream(workingStream, resourcePath))
                {
                    return workingStream;
                }
                else
                {
                    throw new UnauthorizedAccessException("");
                }
            }
            finally
            {
                FinishRead();
            }
        }

        public override Stream GetRawOutputStream(string resourcePath)
        {
            BeginOperator();
            workingStream = new MemoryStream();
            return workingStream;
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
                client.Disconnect();
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
            try
            {
                client.Connect();
                return client.DirectoryExists(resourcePath);
            }
            finally
            {
                client.Disconnect();
            }
        }
        internal void BeginOperator()
        {
            if (busyTag)
            {
                throw new ResourceInUsingException("exist another unfinished operator,wait!");
            }
            busyTag = true;

        }
        private void setFtpClient(FtpClient client)
        {
            if (clientLocal.Value != null && clientLocal.IsValueCreated)
            {

            }
            clientLocal = new ThreadLocal<FtpClient>(()=>client);
        }
        public override void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
            }
            busyTag = false;
        }
    }
}
