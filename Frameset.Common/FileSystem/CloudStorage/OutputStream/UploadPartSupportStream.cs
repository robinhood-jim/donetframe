using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Frameset.Common.FileSystem.CloudStorage.OutputStream
{
    public abstract class UploadPartSupportStream : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => 0;

        public override long Position { get => pos; set => this.pos = value; }
        long pos;

        internal DataCollectionDefine define;
        internal string bucketName;
        internal string region;
        internal string UploadId;
        internal string key;
        internal Dictionary<int, string> etagMap = new Dictionary<int, string>();
        internal Dictionary<int, int> errorPartMap = new Dictionary<int, int>();
        internal Dictionary<int, MemoryStream> partMemMap = new Dictionary<int, MemoryStream>();
        internal int partNum = 0;
        internal int partSize = 20 * 1024 * 1024;
        internal bool finish = false;
        internal void doInit()
        {
            string configPartSizeStr;
            define.ResourceConfig.TryGetValue("fs.UploadPartSize", out configPartSizeStr);
            if (!configPartSizeStr.IsNullOrEmpty())
            {
                partSize = int.Parse(configPartSizeStr);
            }
            initNewPart(0);
        }
        internal void initNewPart(int partNum)
        {
            var currentStream = new MemoryStream(partSize);
            partMemMap.TryAdd(partNum, currentStream);
        }
        public override void Flush()
        {
            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new MethodNotSupportedException("only support write");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new MethodNotSupportedException("only support write");
        }

        public override void SetLength(long value)
        {
            throw new MethodNotSupportedException("only support write");
        }
        public override void Write(byte[] b, int off, int len)
        {

            int offset = off;
            int length = len;
            int size;

            while (length > (size = (int)(partMemMap[partNum].Capacity - pos)))
            {
                partMemMap[partNum].Write(b, offset, size);
                pos += size;
                flushIfNecessary(false);
                offset += size;
                length -= size;
            }
            if (length > 0)
            {
                partMemMap[partNum].Write(b, offset, length);
                pos += length;
            }
        }
        internal void flushIfNecessary(bool force)
        {
            if (UploadId.IsNullOrEmpty())
            {
                initiateUpload();
            }
            if (pos >= partMemMap[partNum].Capacity || force)
            {
                uploadPart(partMemMap[partNum], partNum, pos);
                partNum++;
                initNewPart(partNum);
            }
        }
        public override void Close()
        {
            if (finish)
            {
                return;
            }
            if (!UploadId.IsNullOrEmpty())
            {
                while (etagMap.Count + errorPartMap.Count != partNum)
                {
                    Thread.Sleep(200);
                }
                if (errorPartMap.IsNullOrEmpty())
                {
                    string etag = completeMultiUpload();
                    if (!etag.IsNullOrEmpty())
                    {
                        Log.Information("upload " + key + " with etag {}", etag);
                    }
                    else
                    {
                        Log.Error("upload " + key + " error!");
                    }
                }
                else
                {
                    Log.Error("upload " + key + " error!");
                }
            }
            else
            {
                uploadAsync();
                finish = true;
            }
        }

        internal abstract void initiateUpload();
        internal abstract void uploadPart(MemoryStream stream, int partNum, long size);
        internal abstract void uploadAsync();
        internal abstract string completeMultiUpload();
    }
}
