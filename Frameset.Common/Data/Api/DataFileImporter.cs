using Frameset.Common.Data.Reader;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using System.Collections;
using System.Diagnostics;

namespace Frameset.Common.Data.Api
{
    public static partial class DataFileImporter
    {
        public static AbstractDataIterator<T> GetDataReader<T>(this DataCollectionDefine collectionDefine, string? processPath = null)
        {
            string processFile = processPath ?? collectionDefine.Path;
            FileMeta meta = FileUtil.Parse(processFile);
            Trace.Assert(meta != null);
            collectionDefine.Path = processFile;
            IFileSystem fileSystem = FileSystemFactory.GetFileSystem(collectionDefine);
            return GetEnumerator<T>(collectionDefine, meta, fileSystem);
        }
        public static AbstractDataIterator<T> GetReaderByType<T>(this IFileSystem fileSystem, string processPath)
        {
            Trace.Assert(!processPath.IsNullOrEmpty());
            FileMeta meta = FileUtil.Parse(processPath);
            Trace.Assert(meta != null);
            return Constants.FileFormatTypeOf(meta.FileFormat) switch
            {
                Constants.FileFormatType.CSV => new CsvIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.XML => new XmlIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.JSON => new JsonIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.AVRO => new AvroIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.PARQUET => new ParquetIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.ORC => new OrcIterator<T>(fileSystem, processPath),
                Constants.FileFormatType.XLSX => throw new NotImplementedException(),
                Constants.FileFormatType.ARFF => throw new NotImplementedException(),
                Constants.FileFormatType.PROTOBUF => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };


        }
        private static AbstractDataIterator<T> GetEnumerator<T>(DataCollectionDefine collectionDefine, FileMeta meta, IFileSystem fileSystem)
        {
            return Constants.FileFormatTypeOf(meta.FileFormat) switch
            {
                Constants.FileFormatType.CSV => new CsvIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.XML => new XmlIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.JSON => new JsonIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.AVRO => new AvroIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.PARQUET => new ParquetIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.ORC => new OrcIterator<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.XLSX => throw new NotImplementedException(),
                Constants.FileFormatType.ARFF => throw new NotImplementedException(),
                Constants.FileFormatType.PROTOBUF => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
        }
        public static IEnumerable<T> GetEnumerable<T>(this DataCollectionDefine define, string? processPath = null)
        {
            return new ObjectEnumerable<T>(define, processPath);

        }
        public class ObjectEnumerable<T> : IEnumerable<T>
        {
            private DataCollectionDefine define;
            private IFileSystem fileSystem;
            private FileMeta meta;
            public ObjectEnumerable(DataCollectionDefine define, string processPath)
            {
                this.define = define;
                string processFile = processPath ?? define.Path;
                meta = FileUtil.Parse(processFile);
                fileSystem = FileSystemFactory.GetFileSystem(define);
                if (meta == null)
                {
                    throw new OperationFailedException("file path " + processFile + " parse failed");
                }
                define.Path = processFile;

            }
            public IEnumerator<T> GetEnumerator()
            {
                return GetEnumerator<T>(define, meta, fileSystem);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
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
