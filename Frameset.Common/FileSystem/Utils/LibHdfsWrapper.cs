using System.Runtime.InteropServices;

namespace Frameset.Common.FileSystem.utils
{
    public static partial class LibHdfsWrapper
    {
        private const string LibName = "libhdfs";

        [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr hdfsConnect(string host, int port);

        // 開啟文件，'w' 代表寫入，'a' 代表追加
        [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr hdfsOpenFile(IntPtr fs, string path, int flags, int bufferSize, short replication, long blockSize);

        // 串流寫入數據
        [LibraryImport(LibName)]
        public static partial int hdfsWrite(IntPtr fs, IntPtr file, IntPtr buffer, int length);

        [LibraryImport(LibName)]
        public static partial int hdfsCloseFile(IntPtr fs, IntPtr file);

        [LibraryImport(LibName)]
        public static partial int hdfsDisconnect(IntPtr fs);

        [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int hdfsExists(IntPtr fs, string path);

        [LibraryImport(LibName)]
        public static partial int hdfsRead(IntPtr fs, IntPtr file, IntPtr buffer, int length);

        [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int hdfsCopy(IntPtr fs, string fromPath, IntPtr dfs, string targetPath);

        [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int hdfsMove(IntPtr fs, string fromPath, IntPtr dfs, string targetPath);

        [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int hdfsDelete(IntPtr fs, string fromPath, int recursive);

        [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int hdfsCreateDirectory(IntPtr fs, string directory);

        [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int hdfsSetReplication(IntPtr fs, string path, short replication);

        [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr hdfsListDirectory(IntPtr fs, string path, out int numEntries);

        [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr hdfsGetPathInfo(IntPtr fs, string path);

        [LibraryImport(LibName)]
        public static partial void hdfsFreeFileInfo(IntPtr fileInfo, int numEntries);

        [LibraryImport(LibName)]
        public static partial int hdfsFlush(IntPtr fs, IntPtr hFile);

    }
}
