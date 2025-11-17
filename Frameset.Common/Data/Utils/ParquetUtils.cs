using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using Parquet.Schema;
using System.Diagnostics;

namespace Frameset.Common.Data.Utils
{
    public class ParquetUtils
    {
        internal ParquetUtils()
        {

        }
        public static Type GetValueType(Constants.MetaType type)
        {
            Type retType;
            switch (type)
            {
                case Constants.MetaType.SHORT:
                    retType = typeof(short);
                    break;
                case Constants.MetaType.INTEGER:
                    retType = typeof(int);
                    break;
                case Constants.MetaType.BIGINT:
                    retType = typeof(long);
                    break;
                case Constants.MetaType.DOUBLE:
                    retType = typeof(double);
                    break;
                case Constants.MetaType.FLOAT:
                    retType = typeof(float);
                    break;
                case Constants.MetaType.CLOB:
                case Constants.MetaType.STRING:
                    retType = typeof(string);
                    break;
                case Constants.MetaType.BLOB:
                    retType = typeof(byte[]);
                    break;
                case Constants.MetaType.TIMESTAMP:
                    retType = typeof(DateTime);
                    break;
                case Constants.MetaType.DATE:
                    retType = typeof(DateTime);
                    break;
                default:
                    retType = typeof(string);
                    break;
            }
            return retType;
        }
        public static ParquetSchema GetSchema(DataCollectionDefine MetaDefine, List<DataField> fields)
        {
            Trace.Assert(!MetaDefine.ColumnList.IsNullOrEmpty());
            int pos = 0;
            foreach (DataSetColumnMeta column in MetaDefine.ColumnList)
            {
                DataField field = new DataField(column.ColumnCode, ParquetUtils.GetValueType(column.ColumnType));
                fields.Add(field);

            }
            return new ParquetSchema(fields);
        }
    }
}
