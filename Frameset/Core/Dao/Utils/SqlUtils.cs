using Frameset.Core.Model;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace Frameset.Core.Dao.Utils
{
    public static class SqlUtils
    {
        public static InsertSegment GetInsertSegment(IJdbcDao dao, BaseEntity vo)
        {
            int pos = 1;
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(vo.GetType());
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(vo.GetType());
            StringBuilder builder = new StringBuilder();
            builder.Append("insert into ");
            StringBuilder columnsBuilder = new StringBuilder();
            StringBuilder valuesBuilder = new StringBuilder();
            IList<DbParameter> parameters = new List<DbParameter>();
            if (!entityContent.Schema.IsNullOrEmpty())
            {
                builder.Append(entityContent.Schema).Append(".");
            }
            builder.Append(entityContent.TableName);
            InsertSegment segment = new InsertSegment();
            FieldContent appdenField = null;
            foreach (FieldContent content in fields)
            {
                object realVal = content.GetMethold.Invoke(vo, null);
                if (realVal != null)
                {
                    if (!content.IfIncrement && !content.IfSequence)
                    {
                        string paramName = "@val" + pos++;
                        columnsBuilder.Append(content.FieldName).Append(",");
                        valuesBuilder.Append(paramName).Append(",");
                        parameters.Add(dao.GetDialect().WrapParameter(paramName, realVal));

                    }
                    else if (content.IfSequence)
                    {
                        columnsBuilder.Append(content.FieldName).Append(",");
                        valuesBuilder.Append(dao.GetDialect().GenerateSequenceScript(content.SequenceName)).Append(",");
                        segment.Sequence = true;
                        segment.GenKeyMethod = content.SetMethold;
                        appdenField = content;
                    }
                    else
                    {
                        segment.Increment = true;
                        segment.GenKeyMethod = content.SetMethold;
                    }

                }

            }
            builder.Append("(").Append(columnsBuilder.ToString().Substring(0, columnsBuilder.Length - 1)).Append(") values (")
                .Append(valuesBuilder.ToString().Substring(0, valuesBuilder.Length - 1)).Append(")");
            if (segment.Increment)
            {
                builder.Append(dao.GetDialect().AppendKeyHolder());
            }
            else if (segment.Sequence)
            {
                builder.Append(dao.GetDialect().AppendSequence(appdenField.SequenceName));
            }
            segment.InsertSql = builder.ToString();
            segment.Parameters = parameters;
            return segment;
        }
        public static string GetInsertBactchSql(IJdbcDao dao, Type modelType)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(modelType);
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(modelType);
            StringBuilder builder = new StringBuilder();
            builder.Append("insert into ");
            StringBuilder columnsBuilder = new StringBuilder();
            StringBuilder valuesBuilder = new StringBuilder();
            IList<DbParameter> parameters = new List<DbParameter>();
            if (!entityContent.Schema.IsNullOrEmpty())
            {
                builder.Append(entityContent.Schema).Append(".");
            }
            builder.Append(entityContent.TableName);
            foreach (FieldContent content in fields)
            {
                if (!content.IfIncrement && !content.IfSequence)
                {
                    columnsBuilder.Append(content.FieldName).Append(",");
                    valuesBuilder.Append("?" + content.FieldName).Append(",");
                }
            }
            builder.Append("(").Append(columnsBuilder.ToString().Substring(0, columnsBuilder.Length - 1)).Append(") values (")
                .Append(valuesBuilder.ToString().Substring(0, valuesBuilder.Length - 1)).Append(")");
            return builder.ToString();
        }
        public static UpdateSegment GetUpdateSegment(IJdbcDao dao, BaseEntity vo)
        {
            int pos = 1;
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(vo.GetType());
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(vo.GetType());
            StringBuilder builder = new StringBuilder();
            builder.Append("update ");
            StringBuilder columnsBuilder = new StringBuilder();
            IList<DbParameter> parameters = new List<DbParameter>();
            string whereSegment = " where 1=0";
            UpdateSegment segment = new UpdateSegment();
            if (!entityContent.Schema.IsNullOrEmpty())
            {
                builder.Append(entityContent.Schema).Append(".");
            }
            builder.Append(entityContent.TableName).Append(" set ");

            IList<DbParameter> dbParameters = new List<DbParameter>();
            foreach (FieldContent content in fields)
            {
                object realVal = content.GetMethold.Invoke(vo, null);
                if (realVal != null)
                {
                    if (!content.IfPrimary)
                    {
                        string paramName = "@val" + pos++;
                        columnsBuilder.Append(content.FieldName).Append("=").Append(paramName).Append(",");
                        parameters.Add(dao.GetDialect().WrapParameter(paramName, realVal));
                    }
                    else
                    {
                        whereSegment = " where " + content.FieldName + "=@id";
                        parameters.Add(dao.GetDialect().WrapParameter("@id", realVal));
                    }
                }
                else if (vo.GetDirties().Contains(content.FieldName))
                {
                    columnsBuilder.Append(content.FieldName).Append("=null,");
                }
            }
            segment.UpdateSql = builder.Append(columnsBuilder.ToString().Substring(0, columnsBuilder.Length - 1)).Append(whereSegment).ToString();
            segment.Parameters = parameters;
            return segment;
        }
        public static string GetRemovePkSql(Type modelType)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(modelType);
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(modelType);
            StringBuilder removeBuilder = new StringBuilder("delete from ");
            if (!entityContent.Schema.IsNullOrEmpty())
            {
                removeBuilder.Append(entityContent.Schema).Append(".");
            }
            removeBuilder.Append(entityContent.TableName).Append(" where ");
            foreach (FieldContent content in fields)
            {
                if (content.IfPrimary)
                {
                    removeBuilder.Append(content.FieldName).Append(" in ");
                    break;
                }
            }
            return removeBuilder.ToString();
        }
        public static string GetSelectSql(Type modelType)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(modelType);
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(modelType);
            StringBuilder fieldsBuilder = new StringBuilder();
            StringBuilder tabBuilder = new StringBuilder();
            List<DbParameter> parameters = [];
            if (!entityContent.Schema.IsNullOrEmpty())
            {
                tabBuilder.Append(entityContent.Schema).Append(".");
            }
            tabBuilder.Append(entityContent.TableName);
            foreach (FieldContent content in fields)
            {
                fieldsBuilder.Append(content.FieldName).Append(" as ").Append(content.PropertyName).Append(",");
            }
            return new StringBuilder("select ").Append(fieldsBuilder.ToString().Substring(0, fieldsBuilder.Length - 1)).Append(" from ").Append(tabBuilder).ToString();

        }

        public static string GetSelectSqlAndPk(Type modelType)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(modelType);
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(modelType);
            StringBuilder fieldsBuilder = new StringBuilder();
            StringBuilder tabBuilder = new StringBuilder();

            if (!entityContent.Schema.IsNullOrEmpty())
            {
                tabBuilder.Append(entityContent.Schema).Append(".");
            }
            tabBuilder.Append(entityContent.TableName);
            foreach (FieldContent content in fields)
            {
                fieldsBuilder.Append(content.FieldName).Append(" as ").Append(content.PropertyName).Append(",");
            }
            return new StringBuilder("select ").Append(fieldsBuilder.ToString().Substring(0, fieldsBuilder.Length - 1)).Append(" from ").Append(tabBuilder).ToString();
        }
        public static string GetSelectByIdSql(Type modelType, FieldContent content)
        {

            StringBuilder selectBuilder = new StringBuilder(GetSelectSqlAndPk(modelType));
            selectBuilder.Append(" where ").Append(content.FieldName).Append("=@0");

            return selectBuilder.ToString();
        }
        public static string GetTableWithSchema(EntityContent entityContent)
        {
            StringBuilder tabBuilder = new StringBuilder();
            if (!entityContent.Schema.IsNullOrEmpty())
            {
                tabBuilder.Append(entityContent.Schema).Append(".");
            }
            tabBuilder.Append(entityContent.TableName);
            return tabBuilder.ToString();
        }
    }

    public class InsertSegment
    {
        public string InsertSql
        {
            get; set;
        }
        public IList<DbParameter> Parameters
        {
            get; set;
        }
        public bool Increment
        {
            get; set;
        } = false;
        public bool Sequence
        {
            get; set;
        } = false;
        public MethodInfo GenKeyMethod
        {
            get; set;
        }

    }
    public class UpdateSegment
    {
        public string UpdateSql
        {
            get; set;
        }
        public IList<DbParameter> Parameters
        {
            get; set;
        }
    }
    public class SelectSegment
    {
        public string SelectSql
        {
            get; set;
        }
        public IList<DbParameter> Parameters
        {
            get; set;
        }
    }
}
