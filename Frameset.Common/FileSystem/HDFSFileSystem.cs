using Frameset.Common.Data;
using Frameset.Common.FileSystem.utils;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.Common.FileSystem
{
    public class HDFSFileSystem : AbstractFileSystem
    {
        private HdfsClient client;

        public HDFSFileSystem(DataCollectionDefine define) : base(define)
        {
            identifier = Constants.FileSystemType.HDFS;
            string apiUrl;
            define.ResourceConfig.TryGetValue(ResourceConstants.HDFSBASEURL, out apiUrl);
            client = new HdfsClient(define);

        }


        public override void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
            }
        }

        public override bool Exist(string resourcePath)
        {
            return client.Exists(resourcePath).Result;

        }


        public override Stream? GetInputStream(string resourcePath)
        {
            Stream stream = new MemoryStream();
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

        public override Stream? GetOutputStream(string resourcePath)
        {
            return GetOutputStremWithCompress(resourcePath, new MemoryStream());
        }
        public bool FlushOut(Stream outputStream, string resourcePath)
        {
            return client.WriteStream(outputStream, resourcePath).Result;
        }
        public override Stream? GetRawInputStream(string resourcePath)
        {
            MemoryStream stream = new MemoryStream();
            bool okTag = client.ReadStream(stream, resourcePath).Result;
            if (okTag)
            {
                return stream;
            }
            else
            {
                throw new OperationFailedException("");
            }

        }

        public override Stream? GetRawOutputStream(string resourcePath)
        {
            return new MemoryStream();
        }

        public override Tuple<Stream, StreamReader>? GetReader(string resourcePath)
        {
            throw new NotImplementedException();
        }

        public override long GetStreamSize(string resourcePath)
        {
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
            if (stream != null)
            {
                return Tuple.Create(stream, new StreamWriter(stream));
            }
            else
            {
                return null;
            }
        }

        public override bool IsDirectory(string resourcePath)
        {
            return client.IsDirectory(resourcePath).Result;
        }
    }
}
