using Frameset.Common.Data.Writer;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.Common.Data.Api
{
    public static partial class DataFileExporter
    {
        public static AbstractDataWriter<T> GetDataWriter<T>(this IFileSystem fileSystem, string processPath)
        {
            Trace.Assert(!processPath.IsNullOrEmpty());
            FileMeta meta = FileUtil.Parse(processPath);
            AbstractDataWriter<T> writer = null;
            if (meta != null)
            {
                switch (Constants.FileFormatTypeOf(meta.FileFormat))
                {
                    case Constants.FileFormatType.CSV:
                        writer = new CsvDataWriter<T>(fileSystem, processPath);
                        break;
                    case Constants.FileFormatType.XML:
                        writer = new XmlDataWriter<T>(fileSystem, processPath);
                        break;
                    case Constants.FileFormatType.JSON:
                        writer = new JsonDataWriter<T>(fileSystem, processPath);
                        break;
                    case Constants.FileFormatType.AVRO:
                        writer = new AvroDataWriter<T>(fileSystem, processPath);
                        break;
                    case Constants.FileFormatType.PARQUET:
                        writer = new ParquetDateWriter<T>(fileSystem, processPath);
                        break;
                    case Constants.FileFormatType.ORC:
                        writer = new OrcDataWriter<T>(fileSystem, processPath);
                        break;
                }
            }
            return writer;
        }
        public static AbstractDataWriter<T> GetDataWriter<T>(this DataCollectionDefine collectionDefine, string processPath = null)
        {
            string processFile = processPath ?? collectionDefine.Path;
            FileMeta meta = FileUtil.Parse(processFile);
            IFileSystem fileSystem = FileSystemFactory.GetFileSystem(collectionDefine);
            AbstractDataWriter<T> writer = null;
            if (meta != null)
            {
                switch (Constants.FileFormatTypeOf(meta.FileFormat))
                {
                    case Constants.FileFormatType.CSV:
                        writer = new CsvDataWriter<T>(collectionDefine, fileSystem);
                        break;
                    case Constants.FileFormatType.XML:
                        writer = new XmlDataWriter<T>(collectionDefine, fileSystem);
                        break;
                    case Constants.FileFormatType.JSON:
                        writer = new JsonDataWriter<T>(collectionDefine, fileSystem);
                        break;
                    case Constants.FileFormatType.AVRO:
                        writer = new AvroDataWriter<T>(collectionDefine, fileSystem);
                        break;
                    case Constants.FileFormatType.PARQUET:
                        writer = new ParquetDateWriter<T>(collectionDefine, fileSystem);
                        break;
                    case Constants.FileFormatType.ORC:
                        writer = new OrcDataWriter<T>(collectionDefine, fileSystem);
                        break;
                }
            }
            return writer;
        }
    }
}
