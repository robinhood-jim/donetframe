using Frameset.Core.Dao.Utils;
using Frameset.Core.Model;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Text;

namespace Frameset.Bigdata.Dao
{
    public class SqlGenUtils
    {
        public static InsertSegment GetInsertSegment(BaseEntity vo)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(vo.GetType());
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(vo.GetType());
            StringBuilder builder = new StringBuilder();
            builder.Append("insert into ");
            StringBuilder columnsBuilder = new StringBuilder();
            StringBuilder valuesBuilder = new StringBuilder();
            IList<object> parameters = new List<object>();
            builder.Append(entityContent.GetTableName());
            InsertSegment segment = new InsertSegment();
            foreach (FieldContent content in fields)
            {
                object realVal = content.GetMethod.Invoke(vo, null);
                if (realVal != null)
                {
                    columnsBuilder.Append(content.FieldName).Append(",");
                    valuesBuilder.Append("?,");
                    parameters.Add(realVal);
                }

            }
            segment.InsertSql = builder.ToString();
            segment.ParamObjects = parameters;
            return segment;
        }
        public static UpdateSegment GetUpdateSegment(BaseEntity origin, BaseEntity update)
        {
            Trace.Assert(origin.GetType().Equals(update.GetType()), "compare must be same type");
            int pos = 1;
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(origin.GetType());
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(origin.GetType());
            StringBuilder builder = new StringBuilder();
            builder.Append("update ");
            StringBuilder columnsBuilder = new StringBuilder();
            IList<object> parameters = [];
            string whereSegment = " where 1=0";
            UpdateSegment segment = new UpdateSegment();
            if (!entityContent.Schema.IsNullOrEmpty())
            {
                builder.Append(entityContent.Schema).Append(".");
            }
            builder.Append(entityContent.TableName).Append(" set ");
            foreach (FieldContent content in fields)
            {
                object realVal = content.GetMethod.Invoke(update, null);
                //GetDirty不为空，代表修改字段名在Dirty中，避免非空类型初始化有值导致判断失效
                if ((update.GetDirties().IsNullOrEmpty() || update.GetDirties().Contains(content.PropertyName)) && realVal != null && !string.IsNullOrWhiteSpace(realVal.ToString()))
                {
                    object originVal = content.GetMethod.Invoke(origin, null);
                    if (!content.IfPrimary)
                    {
                        if (!realVal.Equals(originVal))
                        {
                            string paramName = "@val" + pos++;
                            columnsBuilder.Append(content.FieldName).Append("=").Append("?,");
                            parameters.Add(realVal);
                        }
                    }
                    else
                    {
                        whereSegment = " where " + content.FieldName + "=?";
                        parameters.Add(realVal);
                    }
                }
                else if (update.GetDirties().Contains(content.PropertyName))
                {
                    columnsBuilder.Append(content.FieldName).Append("=null,");
                }
            }
            if (columnsBuilder.Length > 0)
            {
                segment.UpdateRequired = true;
            }
            segment.UpdateSql = builder.Append(columnsBuilder.ToString().Substring(0, columnsBuilder.Length - 1)).Append(whereSegment).ToString();
            segment.ParameterObjects = parameters;
            return segment;
        }
    }
}
