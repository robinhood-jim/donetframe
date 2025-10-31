using Frameset.Core.FileSystem;

namespace Frameset.Common.FileSystem
{
    public interface IFileSystem
    {
        Tuple<Stream, StreamReader>? GetReader(string resourcePath);
        Tuple<Stream, StreamWriter>? GetWriter(string resourcePath);
        Stream? GetInputStream(string resourcePath);
        Stream? GetOutputStream(string resourcePath);
        bool Exist(string resourcePath);
        bool IsDirectory(string resourcePath);
        void Init(DataCollectionDefine define);
        long GetStreamSize(string resourcePath);
        Stream? GetRawInputStream(string resourcePath);
        Stream? GetRawOutputStream(string resourcePath);
    }
}
