using Frameset.Common.FileSystem.utils;
using System.Runtime.InteropServices;

namespace Frameset.Common.FileSystem.Utils
{
    public class LibHdfsClient
    {
        public HdfsFileStatus? GetHdfsDetails(IntPtr fs, string path)
        {
            IntPtr ptr = LibHdfsWrapper.hdfsGetPathInfo(fs, path);
            if (ptr == IntPtr.Zero) return null;

            try
            {
                // 將指針轉為結構體
                var info = Marshal.PtrToStructure<HdfsFileInfoStructure>(ptr);
                if (info != null)
                {
                    string fileName = Marshal.PtrToStringAnsi(info.mName) ?? "";
                    long fileSize = info.mSize;
                    HdfsFileStatus status = new();
                    status.Size = fileSize;
                    status.Path = fileName;
                    status.IsDirectory = info.mKind == 0;
                    status.LastAccess = info.mLastAccess;
                    return status;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                LibHdfsWrapper.hdfsFreeFileInfo(ptr, 1);
            }
        }
        public bool Exists(IntPtr fs, string path)
        {
            return GetHdfsDetails(fs, path) != null;
        }

        public List<HdfsFileStatus> ListHdfsDirectory(IntPtr fs, string path)
        {
            int count;
            IntPtr basePtr = LibHdfsWrapper.hdfsListDirectory(fs, path, out count);
            if (basePtr == IntPtr.Zero) return [];

            try
            {
                List<HdfsFileStatus> list = [];
                int structSize = Marshal.SizeOf<HdfsFileInfoStructure>();
                for (int i = 0; i < count; i++)
                {
                    IntPtr currentPtr = (IntPtr)((long)basePtr + (i * structSize));
                    var info = Marshal.PtrToStructure<HdfsFileInfoStructure>(currentPtr);
                    if (info != null)
                    {
                        string? name = Marshal.PtrToStringAnsi(info.mName);
                        long fileSize = info.mSize;
                        HdfsFileStatus status = new();
                        status.Size = fileSize;
                        status.Path = name;
                        status.IsDirectory = info.mKind == 0;
                        status.LastAccess = info.mLastAccess;
                        list.Add(status);
                    }
                }
                return list;
            }
            finally
            {
                LibHdfsWrapper.hdfsFreeFileInfo(basePtr, count);
            }
        }
    }
}
