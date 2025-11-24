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
        public static AbstractDataIterator<T> GetDataReader<T>(this DataCollectionDefine collectionDefine, string processPath = null)
        {
            string processFile = processPath ?? collectionDefine.Path;
            FileMeta meta = FileUtil.Parse(processFile);
            collectionDefine.Path = processFile;
            IFileSystem fileSystem = FileSystemFactory.GetFileSystem(collectionDefine);
            AbstractDataIterator<T> iterator = null;
            if (meta != null)
            {
                iterator = GetEnumerator<T>(collectionDefine, meta, fileSystem);
            }
            else
            {
                throw new OperationFailedException("GetReader failed with Path" + processFile);
            }
            return iterator;
        }
        public static AbstractDataIterator<T> GetReaderByType<T>(this IFileSystem fileSystem, string processPath)
        {
            Trace.Assert(!processPath.IsNullOrEmpty());
            FileMeta meta = FileUtil.Parse(processPath);
            AbstractDataIterator<T> iterator = null;
            if (meta != null)
            {
                switch (Constants.FileFormatTypeOf(meta.FileFormat))
                {
                    case Constants.FileFormatType.CSV:
                        iterator = new CsvIterator<T>(fileSystem, processPath);
                        break;
                    case Constants.FileFormatType.XML:
                        iterator = new XmlIterator<T>(fileSystem, processPath);
                        break;
                    case Constants.FileFormatType.JSON:
                        iterator = new JsonIterator<T>(fileSystem, processPath);
                        break;
                    case Constants.FileFormatType.AVRO:
                        iterator = new AvroIterator<T>(fileSystem, processPath);
                        break;
                    case Constants.FileFormatType.PARQUET:
                        iterator = new ParquetIterator<T>(fileSystem, processPath);
                        break;
                    case Constants.FileFormatType.ORC:
                        iterator = new OrcIterator<T>(fileSystem, processPath);
                        break;

                }
            }
            return iterator;

        }
        private static AbstractDataIterator<T> GetEnumerator<T>(DataCollectionDefine collectionDefine, FileMeta meta, IFileSystem fileSystem)
        {
            AbstractDataIterator<T> iterator = null;
            switch (Constants.FileFormatTypeOf(meta.FileFormat))
            {
                case Constants.FileFormatType.CSV:
                    iterator = new CsvIterator<T>(collectionDefine, fileSystem);
                    break;
                case Constants.FileFormatType.XML:
                    iterator = new XmlIterator<T>(collectionDefine, fileSystem);
                    break;
                case Constants.FileFormatType.JSON:
                    iterator = new JsonIterator<T>(collectionDefine, fileSystem);
                    break;
                case Constants.FileFormatType.AVRO:
                    iterator = new AvroIterator<T>(collectionDefine, fileSystem);
                    break;
                case Constants.FileFormatType.PARQUET:
                    iterator = new ParquetIterator<T>(collectionDefine, fileSystem);
                    break;
                case Constants.FileFormatType.ORC:
                    iterator = new OrcIterator<T>(collectionDefine, fileSystem);
                    break;

            }

            return iterator;
        }
        public static IEnumerable<T> GetEnumerable<T>(this DataCollectionDefine define, string processPath = null)
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
