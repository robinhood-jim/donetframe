using System.Runtime.InteropServices;

namespace Frameset.Common.FileSystem.Utils
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class HdfsFileInfoStructure
    {
        public int mKind;           // tObjectKind: 'F' 為文件, 'D' 為目錄
        public IntPtr mName;        // char* 檔案路徑
        public long mLastMod;       // time_t 最後修改
        public long mSize;          // tOffset 
        public short mReplication;  // 副本數
        public long mBlockSize;     // 區塊大小
        public IntPtr mOwner;       // char*
        public IntPtr mGroup;       // char*
        public short mPermissions;  // 權限
        public long mLastAccess;    // 最後存取時間
    }
}
