using Frameset.Core.FileSystem;

namespace Frameset.Common.FileSystem
{
    /// <summary>
    /// United Data File Access FileSystem Interface
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Read CSV JSON 
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        Tuple<Stream, StreamReader>? GetReader(string resourcePath);
        /// <summary>
        /// Write to CSV and JSON
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        Tuple<Stream, StreamWriter>? GetWriter(string resourcePath);
        /// <summary>
        /// Get InputStream through File Format and CompressType
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        Stream GetInputStream(string resourcePath);
        /// <summary>
        /// Get OutputStream through File Format and CompressType
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        Stream GetOutputStream(string resourcePath);
        /// <summary>
        /// Does resource Exists?
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        bool Exist(string resourcePath);
        bool IsDirectory(string resourcePath);
        /// <summary>
        /// Do init function
        /// </summary>
        /// <param name="define"></param>
        void Init(DataCollectionDefine define);
        /// <summary>
        /// Get Resource Length
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        long GetStreamSize(string resourcePath);
        /// <summary>
        /// Get InputStream through File Format ignore CompressType
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        Stream GetRawInputStream(string resourcePath);
        /// <summary>
        /// Get OutputStream through File Format ignore CompressType
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        Stream GetRawOutputStream(string resourcePath);
        void FinishWrite(Stream outputStream);
        void FinishWrite(Stream outputStream, string path);
    }
}
