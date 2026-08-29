using Apache.Arrow;
using Apache.Arrow.Types;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.Common.Data.Utils
{
    public static class ArrowUtils
    {
        public static Schema GetSchema(DataCollectionDefine collectionDefine)
        {
            List<Field> fields = [];
            if (!collectionDefine.ColumnList.IsNullOrEmpty())
            {
                foreach (DataSetColumnMeta meta in collectionDefine.ColumnList)
                {

                    switch (meta.ColumnType)
                    {
                        case Constants.MetaType.INTEGER:
                            fields.Add(new Field(meta.ColumnCode, Int32Type.Default, false));
                            break;
                        case Constants.MetaType.LONG:
                            fields.Add(new Field(meta.ColumnCode, Int64Type.Default, false));
                            break;
                        case Constants.MetaType.FLOAT:
                            fields.Add(new Field(meta.ColumnCode, FloatType.Default, false));
                            break;
                        case Constants.MetaType.DOUBLE:
                            fields.Add(new Field(meta.ColumnCode, DoubleType.Default, false));
                            break;
                        case Constants.MetaType.SHORT:
                            fields.Add(new Field(meta.ColumnCode, Int16Type.Default, false));
                            break;
                        case Constants.MetaType.TIMESTAMP:
                            fields.Add(new Field(meta.ColumnCode, Int64Type.Default, false));
                            break;
                        case Constants.MetaType.DATE:
                            fields.Add(new Field(meta.ColumnCode, Int64Type.Default, false));
                            break;
                        case Constants.MetaType.STRING:
                            fields.Add(new Field(meta.ColumnCode, StringType.Default, false));
                            break;
                        default:
                            fields.Add(new Field(meta.ColumnCode, StringType.Default, false));
                            break;
                    }
                }
                Schema schema = new Schema(fields, []);
                return schema;
            }
            return null;
        }

    }
}
