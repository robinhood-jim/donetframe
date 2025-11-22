using Frameset.Common.Data.Reader;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.Common.Data.Api
{
    public static partial class DataFileImporter
    {
        public static async IAsyncEnumerator<T> ReadAsyn<T>(DataCollectionDefine collectionDefine, string? processPath = null)
        {
            string processFile = processPath ?? collectionDefine.Path;
            AbstractDataIterator<T> iterator = GetDataReader<T>(collectionDefine, processPath);
            await foreach (var item in iterator.ReadAsync(processFile).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        public static bool IsNotNull(this IFileSystem fileSystem)
        {
            return fileSystem != null;
        }
        public static AbstractDataIterator<T> GetDataReader<T>(this DataCollectionDefine collectionDefine, string processPath = null)
        {
            string processFile = processPath ?? collectionDefine.Path;
            FileMeta meta = FileUtil.Parse(processFile);
            IFileSystem fileSystem = FileSystemFactory.GetFileSystem(collectionDefine);
            AbstractDataIterator<T> iterator = null;
            if (meta != null)
            {
                iterator = GetIterator(collectionDefine, meta, fileSystem, iterator);
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
        private static AbstractDataIterator<T> GetIterator<T>(DataCollectionDefine collectionDefine, FileMeta meta, IFileSystem fileSystem, AbstractDataIterator<T> iterator)
        {
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
    }

}
