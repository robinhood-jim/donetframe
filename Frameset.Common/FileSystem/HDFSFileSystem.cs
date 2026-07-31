using System.Diagnostics;
using Frameset.Common.FileSystem.utils;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.Common.FileSystem
{
    public class HDFSFileSystem : AbstractFileSystem
    {
        private HdfsClient? client = null!;
        private bool useLibhdfs = true;
        private string libhdfsHost;
        private int libhdfsPort;
        private IntPtr fileSystem;
        private IntPtr hFile;

        public HDFSFileSystem(DataCollectionDefine define) : base(define)
        {
            identifier = Constants.FileSystemType.HDFS;
            define.ResourceConfig.TryGetValue(ResourceConstants.HDFSBASEURL, out string? apiUrl);
            if (define.ResourceConfig.TryGetValue(ResourceConstants.HDFSUSEWEBHDFS, out string? useWebHdfsStr))
            {
                useLibhdfs = !string.Equals(Constants.VALID, useWebHdfsStr, StringComparison.OrdinalIgnoreCase);
            }
            if (!useLibhdfs)
            {
                client = new HdfsClient(define);
            }
            else
            {
                define.ResourceConfig.TryGetValue(ResourceConstants.HDFSLIBHDFSHOST, out libhdfsHost);
                if (define.ResourceConfig.TryGetValue(ResourceConstants.HDFSLIBHDFSPORT, out string? portStr))
                {
                    libhdfsPort = Convert.ToInt32(portStr);
                }
                fileSystem = LibHdfsWrapper.hdfsConnect(libhdfsHost, libhdfsPort);
            }
            Init(define);
        }

        protected override void Dispose(bool disposable)
        {
            if (client != null)
            {
                client.Dispose();
            }
            else
            {
                LibHdfsWrapper.hdfsDisconnect(fileSystem);
            }
        }

        public override bool Exist(string resourcePath)
        {
            if (client != null)
            {
                return client.Exists(resourcePath).Result;
            }
            else
            {
                return false;
            }

        }


        public override Stream GetInputStream(string resourcePath)
        {
            Stream stream = new MemoryStream();
            Trace.Assert(client!=null,"");
            bool oktag = client.ReadStream(stream, resourcePath).Result;
            if (oktag)
            {
                return stream;
            }
            else
            {
                throw new OperationFailedException("");
            }
        }

        public override Stream GetOutputStream(string resourcePath)
        {
            Stream outputStream = new MemoryStream();
            return GetOutputStremWithCompress(resourcePath, outputStream);
        }
        public override void FinishWrite(Stream? outputStream, string path)
        {
            if (outputStream != null)
            {
                FlushOut(outputStream, path);
            }

        }
        internal bool FlushOut(Stream outputStream, string resourcePath)
        {
            Trace.Assert(client!=null,"");
            return client.WriteStream(outputStream, resourcePath).Result;
        }
        public override Stream GetRawInputStream(string resourcePath)
        {
            Stream outputStream = new MemoryStream();
            Trace.Assert(client!=null,"");
            bool okTag = client.ReadStream(outputStream, resourcePath).Result;
            if (okTag)
            {
                return outputStream;
            }
            else
            {
                throw new OperationFailedException("");
            }

        }

        public override Stream GetRawOutputStream(string resourcePath)
        {
            return new MemoryStream();
        }

        public override Tuple<Stream, StreamReader> GetReader(string resourcePath)
        {
            Stream input = GetInputStream(resourcePath);
            
            return Tuple.Create(input, new StreamReader(input));
           
        }

        public override long GetStreamSize(string resourcePath)
        {
            Trace.Assert(client!=null,"");
            Dictionary<string, object> contentMap = client.ContentSummary(resourcePath).Result;
            if (!contentMap.IsNullOrEmpty())
            {
                return long.Parse(contentMap["length"].ToString());
            }
            else
            {
                throw new OperationFailedException("");
            }
        }

        public override Tuple<Stream, StreamWriter>? GetWriter(string resourcePath)
        {
            Stream stream = GetOutputStream(resourcePath);
            return Tuple.Create(stream, new StreamWriter(stream));
        }

        public override bool IsDirectory(string resourcePath)
        {
            Trace.Assert(client!=null,"");
            return client.IsDirectory(resourcePath).Result;
        }
    }
}
