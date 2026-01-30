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
            Trace.Assert(meta != null);
            return Constants.FileFormatTypeOf(meta.FileFormat) switch
            {
                Constants.FileFormatType.CSV => new CsvDataWriter<T>(fileSystem, processPath),
                Constants.FileFormatType.XML => new XmlDataWriter<T>(fileSystem, processPath),
                Constants.FileFormatType.JSON => new JsonDataWriter<T>(fileSystem, processPath),
                Constants.FileFormatType.AVRO => new AvroDataWriter<T>(fileSystem, processPath),
                Constants.FileFormatType.PARQUET => new ParquetDateWriter<T>(fileSystem, processPath),
                Constants.FileFormatType.ORC => new OrcDataWriter<T>(fileSystem, processPath),
                Constants.FileFormatType.XLSX => throw new NotImplementedException(),
                Constants.FileFormatType.ARFF => throw new NotImplementedException(),
                Constants.FileFormatType.PROTOBUF => new ProtoBufWriter<T>(fileSystem, processPath),
                _ => throw new NotImplementedException()
            };

        }
        public static AbstractDataWriter<T> GetDataWriter<T>(this DataCollectionDefine collectionDefine, string? processPath = null)
        {
            string processFile = processPath ?? collectionDefine.Path;
            FileMeta meta = FileUtil.Parse(processFile);
            Trace.Assert(meta != null);
            collectionDefine.Path = processFile;
            IFileSystem fileSystem = FileSystemFactory.GetFileSystem(collectionDefine);
            return GetWriterByFormat<T>(collectionDefine, meta, fileSystem);
        }

        private static AbstractDataWriter<T> GetWriterByFormat<T>(DataCollectionDefine collectionDefine, FileMeta meta, IFileSystem fileSystem)
        {
            return Constants.FileFormatTypeOf(meta.FileFormat) switch
            {
                Constants.FileFormatType.CSV => new CsvDataWriter<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.XML => new XmlDataWriter<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.JSON => new JsonDataWriter<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.AVRO => new AvroDataWriter<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.PARQUET => new ParquetDateWriter<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.ORC => new OrcDataWriter<T>(collectionDefine, fileSystem),
                Constants.FileFormatType.XLSX => throw new NotImplementedException(),
                Constants.FileFormatType.ARFF => throw new NotImplementedException(),
                Constants.FileFormatType.PROTOBUF => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
        }
    }
}
