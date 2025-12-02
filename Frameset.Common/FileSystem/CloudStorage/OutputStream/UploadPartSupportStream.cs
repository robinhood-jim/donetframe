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

        public override bool CanWrite => writeTag;

        public override long Length => 0;

        public override long Position { get => pos; set => this.pos = value; }
        long pos;

        protected DataCollectionDefine define;
        protected string bucketName;
        protected string region = null!;
        protected string UploadId = null!;
        protected string key;
        protected Dictionary<int, string> etagMap = [];
        protected Dictionary<int, int> errorPartMap = [];
        protected Dictionary<int, MemoryStream> partMemMap = [];
        protected int partNum = 0;
        protected int partSize = 20 * 1024 * 1024;
        protected bool finish = false;
        protected bool writeTag = true;
        protected UploadPartSupportStream(DataCollectionDefine define, string bucketName, string key)
        {
            this.define = define;
            this.bucketName = bucketName;
            this.key = key;
        }
        protected void doInit()
        {
            define.ResourceConfig.TryGetValue("fs.UploadPartSize", out string? configPartSizeStr);
            if (!configPartSizeStr.IsNullOrEmpty())
            {
                partSize = int.Parse(configPartSizeStr);
            }
            initNewPart(0);
        }
        protected void initNewPart(int partNum)
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
                FlushIfNecessary(false);
                offset += size;
                length -= size;
            }
            if (length > 0)
            {
                partMemMap[partNum].Write(b, offset, length);
                pos += length;
            }
        }
        protected void FlushIfNecessary(bool force)
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
                        Log.Information("upload " + key + " with etag {Etag}", etag);
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
                writeTag = false;
            }
        }

        protected abstract void initiateUpload();
        protected abstract void uploadPart(MemoryStream stream, int partNum, long size);
        protected abstract void uploadAsync();
        protected abstract string completeMultiUpload();
    }
}
