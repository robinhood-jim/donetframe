using Frameset.Common.Data.Reader;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.Common.Data.Api
{
    public static class DataFileImporter
    {
        public static AbstractDataIterator<T> GetDataReader<T>(this DataCollectionDefine collectionDefine, string? processPath = null, char? pathSepearatorStr = null, Action<AbstractDataIterator<T>>? initFunc = null)
        {
            string processFile = processPath ?? collectionDefine.Path;
            char pathSeparator = pathSepearatorStr != null ? pathSepearatorStr.Value : Path.DirectorySeparatorChar;
            FileMeta meta = collectionDefine.MetaData;
            Constants.FileFormatType fileFormat = collectionDefine.FileFormat;
            if (meta == null)
            {
                meta = FileUtil.Parse(processFile, pathSeparator);
                collectionDefine.MetaData = meta;
                fileFormat = Constants.FileFormatTypeOf(meta.FileFormat);
                collectionDefine.FileFormat = fileFormat;
            }
            Trace.Assert(meta != null);
            collectionDefine.Path = processFile;
            IFileSystem fileSystem = FileSystemFactory.GetFileSystem(collectionDefine);
            return GetEnumerator<T>(collectionDefine, fileFormat, fileSystem, initFunc);
        }
        public static AbstractDataIterator<T> GetReaderByType<T>(this IFileSystem fileSystem, string processPath, char? pathSepearatorStr = null, Action<AbstractDataIterator<T>>? initFunc = null)
        {
            Trace.Assert(!processPath.IsNullOrEmpty());
            char pathSeparator = pathSepearatorStr != null ? pathSepearatorStr.Value : Path.DirectorySeparatorChar;
            FileMeta meta = FileUtil.Parse(processPath, pathSeparator);
            Trace.Assert(meta != null);
            return Constants.FileFormatTypeOf(meta.FileFormat) switch
            {
                Constants.FileFormatType.CSV => new CsvIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.XML => new XmlIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.JSON => new JsonIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.AVRO => new AvroIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.PARQUET => new ParquetIterator<T>(fileSystem, processPath, initFunc),
                Constants.FileFormatType.ORC => new OrcIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.XLSX => new XlsxIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.ARFF => throw new NotImplementedException(),
                Constants.FileFormatType.PROTO => new ParquetIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.ARROW => new ArrowIterator<T>(fileSystem, processPath),
                _ => throw new NotImplementedException()
            };


        }
        internal static AbstractDataIterator<T> GetEnumerator<T>(DataCollectionDefine collectionDefine, Constants.FileFormatType formatType, IFileSystem fileSystem, Action<AbstractDataIterator<T>>? initFunc)
        {
            return formatType switch
            {
                Constants.FileFormatType.CSV => new CsvIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.XML => new XmlIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.JSON => new JsonIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.AVRO => new AvroIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.PARQUET => new ParquetIterator<T>(collectionDefine, fileSystem, initFunc),
                Constants.FileFormatType.ORC => new OrcIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.XLSX => new XlsxIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.ARFF => throw new NotImplementedException(),
                Constants.FileFormatType.PROTO => new ProtobufIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.ARROW => new ArrowIterator<T>(collectionDefine, fileSystem),
                _ => throw new NotImplementedException()
            };
        }
        public static ObjectEnumerable<T> GetEnumerable<T>(this DataCollectionDefine define, string? processPathStr = null, char? pathSeparator = null, Action<AbstractDataIterator<T>>? initFunc = null)
        {
            string processPath = processPathStr ?? define.Path;
            return new ObjectEnumerable<T>(define, processPath, pathSeparator, initFunc);
        }

        public static async IAsyncEnumerator<T> ReadAsyn<T>(DataCollectionDefine collectionDefine, string? processPath = null)
        {
            string processFile = processPath ?? collectionDefine.Path;
            AbstractDataIterator<T> iterator = GetDataReader<T>(collectionDefine, processPath);
            await foreach (var item in iterator.ReadAsync(processFile).ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }

}
