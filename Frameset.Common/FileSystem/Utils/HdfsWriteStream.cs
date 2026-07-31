using Frameset.Common.Exceptions;
using Frameset.Common.FileSystem.utils;

namespace Frameset.Common.FileSystem.Utils
{
    public class HdfsWriteStream : Stream
    {
        private readonly IntPtr fs;
        private readonly IntPtr hFile;
        private bool disposed;
        public HdfsWriteStream(IntPtr fs, string path, int bufferSize = 1048576, short replication = 3, long blockSize = 134217728)
        {
            this.fs = fs;
            hFile = LibHdfsWrapper.hdfsOpenFile(fs, path, 1, bufferSize, replication, blockSize);
            if (hFile == IntPtr.Zero)
            {
                throw new HdfsException($" path {path} can not write!");
            }
        }
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            LibHdfsWrapper.hdfsFlush(fs, hFile);

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public unsafe override void Write(byte[] buffer, int offset, int count)
        {
            fixed (byte* pBuf = &buffer[offset])
            {
                int result = LibHdfsWrapper.hdfsWrite(fs, hFile, (IntPtr)pBuf, count);
                if (result < 0)
                {
                    throw new HdfsException("Hdfs write failed!");
                }
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
    }
}
