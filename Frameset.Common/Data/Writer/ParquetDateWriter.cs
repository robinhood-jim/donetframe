using Frameset.Common.Data.Utils;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using Parquet;
using Parquet.Schema;
using System.Collections;

namespace Frameset.Common.Data.Writer
{
    public class ParquetDateWriter<T> : AbstractDataWriter<T>
    {
        private ParquetWriter pwriter=null!;
        private ParquetRowGroupWriter groupWriter=null!;
        private ParquetSchema schema=null!;
        private List<DataField> fields = new();
        private Dictionary<int, ArrayList> chunckMap = new();
        private int chunckCapcity = 20000;
        private long totalRow = 0;

        public ParquetDateWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.PARQUET;
            useRawOutputStream = true;
            Initalize();
        }

        public ParquetDateWriter(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.PARQUET;
            useRawOutputStream = true;
            Initalize();
        }

        internal override void Initalize()
        {
            base.Initalize();
            if (MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.PARQUETGROUPSIZE, out string? chunckSizeStr))
            {
                if (!chunckSizeStr.IsNullOrEmpty())
                {
                    chunckCapcity = int.Parse(chunckSizeStr);
                }
            }
            schema = ParquetUtils.GetSchema(MetaDefine, fields);
            for (int pos = 0; pos < MetaDefine.ColumnList.Count; pos++)
            {
                chunckMap.TryAdd(pos, new ArrayList(chunckCapcity));
            }
            pwriter = ParquetWriter.CreateAsync(schema, outputStream).Result;
            CompressType compressType = GetCompressType();
            CompressionMethod method = compressType switch
            {
                CompressType.SNAPPY => CompressionMethod.Snappy,
                CompressType.GZ => CompressionMethod.Gzip,
                CompressType.LZO => CompressionMethod.Lzo,
                CompressType.LZ4 => CompressionMethod.LZ4,
                CompressType.ZSTD => CompressionMethod.Zstd,
                CompressType.BROTLI => CompressionMethod.Brotli,
                CompressType.LZMA => CompressionMethod.Lz4Raw,
                _ => CompressionMethod.None
            };

            pwriter.CompressionMethod = method;
        }


        public override void FinishWrite()
        {
            if (groupWriter != null)
            {
                FlushGroup();
                groupWriter.Dispose();
            }
            pwriter.Dispose();
        }

        public override void WriteRecord(T value)
        {
            groupWriter ??= pwriter.CreateRowGroup();
            for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
            {
                object? retVal = GetValue(value, MetaDefine.ColumnList[i]);
                chunckMap[i].Add(retVal);
            }
            totalRow++;
            if (totalRow % chunckCapcity == 0)
            {
                FlushGroup();
                Flush();
                groupWriter.Dispose();
            }
        }
        internal bool FlushGroup()
        {
            List<Task> tasks = new();
            for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
            {
                Array array = chunckMap[i].ToArray(fields[i].ClrType);
                tasks.Add(groupWriter.WriteColumnAsync(new Parquet.Data.DataColumn(fields[i], array)));
            }
            foreach (var item in tasks)
            {
                item.GetAwaiter().GetResult();
            }

            for (int pos = 0; pos < MetaDefine.ColumnList.Count; pos++)
            {
                chunckMap[pos].Clear();
            }
            return true;
        }
    }
}
