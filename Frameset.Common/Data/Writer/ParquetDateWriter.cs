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
        private ParquetWriter pwriter;
        private ParquetRowGroupWriter groupWriter;
        private ParquetSchema schema;
        private List<DataField> fields = new List<DataField>();
        private Dictionary<int, ArrayList> chunckMap = new Dictionary<int, ArrayList>();
        private int chunckCapcity = 2000;
        private long totalRow = 0;

        public ParquetDateWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.PARQUET;
            useRawOutputStream = true;
            initalize();
        }

        public ParquetDateWriter(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.PARQUET;
            useRawOutputStream = true;
            initalize();
        }

        internal override void initalize()
        {
            base.initalize();
            string chunckSizeStr;
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.PARQUETGROUPSIZE, out chunckSizeStr);
            if (!chunckSizeStr.IsNullOrEmpty())
            {
                chunckCapcity = int.Parse(chunckSizeStr);
            }
            schema = ParquetUtils.GetSchema(MetaDefine, fields);
            for (int pos = 0; pos < MetaDefine.ColumnList.Count; pos++)
            {
                chunckMap.TryAdd(pos, new ArrayList(chunckCapcity));
            }
            pwriter = ParquetWriter.CreateAsync(schema, outputStream).Result;
            CompressType compressType = GetCompressType();
            CompressionMethod method = CompressionMethod.None;
            switch (compressType)
            {
                case CompressType.SNAPPY:
                    method = CompressionMethod.Snappy;
                    break;
                case CompressType.GZ:
                    method = CompressionMethod.Gzip;
                    break;
                case CompressType.LZO:
                    method = CompressionMethod.Lzo;
                    break;
                case CompressType.LZ4:
                    method = CompressionMethod.LZ4;
                    break;
                case CompressType.ZSTD:
                    method = CompressionMethod.Zstd;
                    break;
                case CompressType.BROTLI:
                    method = CompressionMethod.Brotli;
                    break;
                case CompressType.LZMA:
                    method = CompressionMethod.Lz4Raw;
                    break;

            }
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
            if (groupWriter == null)
            {
                groupWriter = pwriter.CreateRowGroup();
            }
            for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
            {
                object retVal = GetValue(value, MetaDefine.ColumnList[i]);
                chunckMap[i].Add(retVal);
            }
            totalRow++;
            if (totalRow % chunckCapcity == 0)
            {
                FlushGroup();
                Flush();
                groupWriter.Dispose();
                groupWriter = null;
            }
        }
        internal bool FlushGroup()
        {
            List<Task> tasks = new List<Task>();
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
