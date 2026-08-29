using Frameset.Common.Exceptions;
using Frameset.Common.FileSystem.utils;

namespace Frameset.Common.FileSystem.Utils
{
    public class HdfsReadStream : Stream
    {
        private readonly IntPtr fs;
        private readonly IntPtr hFile;
        private bool disposed;
        public HdfsReadStream(IntPtr fs, string path, int bufferSize = 1048576, short replication = 3, long blockSize = 134217728)
        {
            this.fs = fs;
            hFile = LibHdfsWrapper.hdfsOpenFile(fs, path, 1, bufferSize, replication, bufferSize);
            if (hFile == IntPtr.Zero)
            {
                throw new HdfsException($" path {path} does't exists!");
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                LibHdfsWrapper.hdfsCloseFile(fs, hFile);
                disposed = true;
            }
            base.Dispose(disposing);
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {

        }

        public unsafe override int Read(byte[] buffer, int offset, int count)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(HdfsReadStream));
            }
            fixed (byte* pBuf = &buffer[offset])
            {
                int bytesRead = LibHdfsWrapper.hdfsRead(fs, hFile, (IntPtr)pBuf, count);

                if (bytesRead < 0) throw new IOException("HDFS read error");
                return bytesRead;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
