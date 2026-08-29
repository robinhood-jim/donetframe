using Apache.Arrow;
using Apache.Arrow.Ipc;
using Frameset.Common.Data.Utils;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using Apache.Arrow.Memory;

namespace Frameset.Common.Data.Writer
{
    public class ArrowWrite<T> : AbstractDataWriter<T>
    {
        private Schema schema=null!;
        private ArrowStreamWriter arrowWriter=null!;
        private int chunckCapcity = 10000;
        private long totalRow = 0;
        private int groupRow = 0;
        private List<object> buiders = [];
        private IpcOptions ipcOptions = new IpcOptions();
        public ArrowWrite(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.ARROW;
            useRawOutputStream = true;
            Initalize();
        }
        public ArrowWrite(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.ARROW;
            useRawOutputStream = true;
            Initalize();
        }
        internal override void Initalize()
        {
            base.Initalize();
            schema = ArrowUtils.GetSchema(MetaDefine);
            //ipcOptions.CompressionCodec = CompressionCodecType.Zstd;
            arrowWriter = new ArrowStreamWriter(outputStream, schema, false, ipcOptions);
            ConstructBuilder();
            if (MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.ARROWGROUPSIZE, out string? chunckSizeStr))
            {
                if (!chunckSizeStr.IsNullOrEmpty())
                {
                    chunckCapcity = int.Parse(chunckSizeStr);
                }
            }
        }
        internal void ConstructBuilder()
        {
            foreach (DataSetColumnMeta meta in MetaDefine.ColumnList)
            {
                switch (meta.ColumnType)
                {
                    case Constants.MetaType.INTEGER:
                        buiders.Add(new Int32Array.Builder());
                        break;
                    case Constants.MetaType.LONG:
                        buiders.Add(new Int64Array.Builder());
                        break;
                    case Constants.MetaType.SHORT:
                        buiders.Add(new Int16Array.Builder());
                        break;
                    case Constants.MetaType.FLOAT:
                        buiders.Add(new FloatArray.Builder());
                        break;
                    case Constants.MetaType.DOUBLE:
                        buiders.Add(new DoubleArray.Builder());
                        break;
                    case Constants.MetaType.TIMESTAMP:
                        buiders.Add(new Int64Array.Builder());
                        break;
                    case Constants.MetaType.DATE:
                        buiders.Add(new Int64Array.Builder());
                        break;
                    case Constants.MetaType.STRING:
                        buiders.Add(new StringArray.Builder());
                        break;
                    default:
                        buiders.Add(new StringArray.Builder());
                        break;

                }
            }
        }
        internal void WriteColumn(DataSetColumnMeta meta, int columnPos, object value)
        {
            switch (meta.ColumnType)
            {
                case Constants.MetaType.SHORT:
                    ((Int16Array.Builder)buiders[columnPos]).Append(Convert.ToInt16(value));
                    break;
                case Constants.MetaType.INTEGER:
                    ((Int32Array.Builder)buiders[columnPos]).Append(Convert.ToInt32(value));
                    break;
                case Constants.MetaType.LONG:
                    ((Int64Array.Builder)buiders[columnPos]).Append(Convert.ToInt64(value));
                    break;
                case Constants.MetaType.FLOAT:
                    ((FloatArray.Builder)buiders[columnPos]).Append(float.Parse(value.ToString(), CultureInfo.InvariantCulture));
                    break;
                case Constants.MetaType.DOUBLE:
                    ((DoubleArray.Builder)buiders[columnPos]).Append(Convert.ToDouble(value));
                    break;
                case Constants.MetaType.TIMESTAMP:
                    DateTimeOffset? dt = null;
                    long tsValue = 0;
                    if (value is DateTime)
                    {
                        dt = new DateTimeOffset((DateTime)value);
                    }
                    else if (value is DateTimeOffset)
                    {
                        dt = (DateTimeOffset)value;
                    }
                    if (dt != null)
                    {
                        tsValue = dt.Value.ToUnixTimeMilliseconds();
                    }
                    else
                    {
                        tsValue = Convert.ToInt64(value);
                    }
                    ((Int64Array.Builder)buiders[columnPos]).Append(tsValue);
                    break;
                case Constants.MetaType.STRING:
                    ((StringArray.Builder)buiders[columnPos]).Append(value.ToString());
                    break;
                default:
                    ((StringArray.Builder)buiders[columnPos]).Append(value.ToString());
                    break;
            }
        }
        internal List<IArrowArray> ConstructBatch()
        {
            List<IArrowArray> retList = [];
            for (int i = 0; i < buiders.Count; i++)
            {
                DataSetColumnMeta meta = MetaDefine.ColumnList[i];
                switch (meta.ColumnType)
                {
                    case Constants.MetaType.SHORT:
                        retList.Add(((Int16Array.Builder)buiders[i]).Build());
                        break;
                    case Constants.MetaType.INTEGER:
                        retList.Add(((Int32Array.Builder)buiders[i]).Build());
                        break;
                    case Constants.MetaType.LONG:
                        retList.Add(((Int64Array.Builder)buiders[i]).Build());
                        break;
                    case Constants.MetaType.FLOAT:
                        retList.Add(((FloatArray.Builder)buiders[i]).Build());
                        break;
                    case Constants.MetaType.DOUBLE:
                        retList.Add(((DoubleArray.Builder)buiders[i]).Build());
                        break;
                    case Constants.MetaType.TIMESTAMP:
                        retList.Add(((Int64Array.Builder)buiders[i]).Build());
                        break;
                    case Constants.MetaType.STRING:
                        retList.Add(((StringArray.Builder)buiders[i]).Build());
                        break;
                    default:
                        retList.Add(((StringArray.Builder)buiders[i]).Build());
                        break;
                }
            }
            return retList;
        }
        internal void ClearBuilder()
        {
            for (int i = 0; i < buiders.Count; i++)
            {
                DataSetColumnMeta meta = MetaDefine.ColumnList[i];
                switch (meta.ColumnType)
                {
                    case Constants.MetaType.SHORT:
                        ((Int16Array.Builder)buiders[i]).Clear();
                        break;
                    case Constants.MetaType.INTEGER:
                        ((Int32Array.Builder)buiders[i]).Clear();
                        break;
                    case Constants.MetaType.LONG:
                        ((Int64Array.Builder)buiders[i]).Clear();
                        break;
                    case Constants.MetaType.FLOAT:
                        ((FloatArray.Builder)buiders[i]).Clear();
                        break;
                    case Constants.MetaType.DOUBLE:
                        ((DoubleArray.Builder)buiders[i]).Clear();
                        break;
                    case Constants.MetaType.TIMESTAMP:
                        ((Int64Array.Builder)buiders[i]).Clear();
                        break;
                    case Constants.MetaType.STRING:
                        ((StringArray.Builder)buiders[i]).Clear();
                        break;
                    default:
                        ((StringArray.Builder)buiders[i]).Clear();
                        break;
                }
            }
        }

        public override void FinishWrite()
        {
            if (arrowWriter != null)
            {
                if (groupRow > 0)
                {
                    FlushGroup();
                }
                ClearBuilder();
                arrowWriter.WriteEnd();
                Flush();
                arrowWriter.Dispose();
            }
        }
        internal void FlushGroup()
        {
            using RecordBatch recordBatch = new RecordBatch(schema, ConstructBatch(), groupRow);
            groupRow = 0;
            arrowWriter.WriteRecordBatch(recordBatch);
            ClearBuilder();
        }

        public override void WriteRecord(T value)
        {

            for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
            {
                object? retVal = GetValue(value, MetaDefine.ColumnList[i]);
                if (retVal != null)
                {
                    WriteColumn(MetaDefine.ColumnList[i], i, retVal);
                }
            }
            totalRow++;
            groupRow++;
            if (totalRow % chunckCapcity == 0)
            {
                FlushGroup();
                Flush();
            }

        }
    }
}
