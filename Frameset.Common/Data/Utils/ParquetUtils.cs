using Frameset.Core.Dao;
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

        public static ParquetSchema GetSchema(DataCollectionDefine MetaDefine, List<DataField> fields)
        {
            Trace.Assert(!MetaDefine.ColumnList.IsNullOrEmpty());
            foreach (DataSetColumnMeta column in MetaDefine.ColumnList)
            {
                DataField field = new DataField(column.ColumnCode, DataMetaUtils.GetValueType(column.ColumnType));
                fields.Add(field);

            }
            return new ParquetSchema(fields);
        }
    }
}
