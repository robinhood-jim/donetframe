using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.Model;
using Frameset.Core.Repo;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
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
                object realVal = content.GetMethod.Invoke(vo, null);
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
                        segment.GenKeyMethod = content.SetMethod;
                        appdenField = content;
                    }
                    else
                    {
                        segment.Increment = true;
                        segment.GenKeyMethod = content.SetMethod;
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

            builder.Append(entityContent.GetTableName());
            foreach (FieldContent content in fields)
            {
                if (!content.IfIncrement && !content.IfSequence && content.ParamType.IsPrimitive)
                {
                    columnsBuilder.Append(content.FieldName).Append(",");
                    valuesBuilder.Append("?" + content.FieldName).Append(",");
                }
            }
            builder.Append("(").Append(columnsBuilder.ToString().Substring(0, columnsBuilder.Length - 1)).Append(") values (")
                .Append(valuesBuilder.ToString().Substring(0, valuesBuilder.Length - 1)).Append(")");
            return builder.ToString();
        }
        public static UpdateSegment GetUpdateSegment(IJdbcDao dao, BaseEntity origin, BaseEntity update)
        {
            Trace.Assert(origin.GetType().Equals(update.GetType()), "compare must be same type");
            int pos = 1;
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(origin.GetType());
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(origin.GetType());
            StringBuilder builder = new StringBuilder();
            builder.Append("update ");
            StringBuilder columnsBuilder = new StringBuilder();
            IList<DbParameter> parameters = new List<DbParameter>();
            string whereSegment = " where 1=0";
            UpdateSegment segment = new UpdateSegment();

            builder.Append(entityContent.GetTableName()).Append(" set ");
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
                            columnsBuilder.Append(content.FieldName).Append("=").Append(paramName).Append(",");
                            parameters.Add(dao.GetDialect().WrapParameter(paramName, realVal));
                        }
                    }
                    else
                    {
                        whereSegment = " where " + content.FieldName + "=@id";
                        parameters.Add(dao.GetDialect().WrapParameter("@id", realVal));
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
            segment.Parameters = parameters;
            return segment;
        }
        public static string GetRemovePkSql(Type modelType)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(modelType);
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(modelType);
            StringBuilder removeBuilder = new StringBuilder("delete from ");

            removeBuilder.Append(entityContent.GetTableName()).Append(" where ");
            foreach (FieldContent content in fields)
            {
                if (content.IfPrimary)
                {
                    removeBuilder.Append(content.FieldName).Append(" in (");
                    break;
                }
            }
            return removeBuilder.ToString();
        }
        public static Tuple<StringBuilder, IList<DbParameter>> GetRemoveCondition(IJdbcDao dao, Type modelType, string fieldName, Constants.SqlOperator oper, object[] values)
        {
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(modelType);
            Dictionary<string, FieldContent> fieldMaps = EntityReflectUtils.GetFieldsMap(modelType);
            if (!fieldMaps.TryGetValue(fieldName, out FieldContent fieldContent))
            {
                throw new BaseSqlException("field " + fieldName + " not found in entity");
            }
            StringBuilder builder = new StringBuilder("DELETE FROM ").Append(entityContent.GetTableName()).Append(" WHERE ");

            IList<DbParameter> parameters = ParameterHelper.AddQueryParam(dao, fieldContent, builder, 0, out int _, oper, values);
            return Tuple.Create(builder, parameters);
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
                if (!content.IsManyToOne && !content.IsOneToMany)
                {
                    fieldsBuilder.Append(content.FieldName).Append(" as ").Append(content.PropertyName).Append(",");
                }
            }
            return new StringBuilder("select ").Append(fieldsBuilder.ToString().Substring(0, fieldsBuilder.Length - 1)).Append(" from ").Append(tabBuilder).ToString();

        }


        public static string GetSelectByIdSql(Type modelType, FieldContent content)
        {

            StringBuilder selectBuilder = new StringBuilder(GetSelectSql(modelType));
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
        public static void AppendPreparedSql(FilterCondition condition, Dictionary<string, object> valueMap, StringBuilder builder, Dictionary<string, int> duplicatedMap, Dictionary<Type, string> entityAliasMap)
        {
            Trace.Assert(!condition.Values.IsNullOrEmpty(), "");
            StringBuilder aliasNameBuilder = new StringBuilder();
            if (!entityAliasMap.IsNullOrEmpty() && entityAliasMap.TryGetValue(condition.TargetEntity.EntityType, out string aliasName))
            {
                aliasNameBuilder.Append(aliasName).Append('.');
            }
            builder.Append(aliasNameBuilder).Append(condition.ColumnName);
            builder.Append(Constants.OperatorValue(condition.Operator));
            duplicatedMap.TryGetValue(condition.ColumnName, out int counts);
            string fieldName = condition.ColumnName + Convert.ToString(counts);
            Type ColumnType = condition.ColumnType;
            List<object> Values = condition.Values;
            Constants.SqlOperator Operator = condition.Operator;
            if (counts == 0)
            {
                duplicatedMap.TryAdd(condition.ColumnName, ++counts);
            }
            else
            {
                duplicatedMap[condition.ColumnName] = ++counts;
            }

            switch (Operator)
            {
                case Constants.SqlOperator.EQ:
                case Constants.SqlOperator.NE:
                case Constants.SqlOperator.LT:
                case Constants.SqlOperator.GE:
                case Constants.SqlOperator.GT:
                    if (!condition.AllArith)
                    {
                        builder.Append("@" + fieldName);
                        valueMap.TryAdd(fieldName, ConvertUtil.ParseByType(condition.ColumnType, condition.Values[0]));
                    }
                    else
                    {
                        builder.Append(condition.RightArithmetic);
                    }
                    break;
                case Constants.SqlOperator.BT:
                    Trace.Assert(condition.Values.IsNullOrEmpty() && condition.Values.Count >= 2, "");
                    builder.Append("@" + condition.ColumnName + "From AND @" + condition.ColumnName + "To");
                    valueMap.TryAdd(fieldName + "From", ConvertUtil.ParseByType(ColumnType, Values[0]));
                    valueMap.TryAdd(fieldName + "To", ConvertUtil.ParseByType(ColumnType, Values[1]));
                    break;
                case Constants.SqlOperator.LIKE:
                case Constants.SqlOperator.LLIKE:
                case Constants.SqlOperator.RLIKE:
                    valueMap.TryAdd(fieldName, Values[0].ToString());
                    if (Constants.SqlOperator.LLIKE == Operator)
                    {
                        builder.Append(" '%'+@" + fieldName);

                    }
                    else if (Constants.SqlOperator.RLIKE == Operator)
                    {
                        builder.Append("@" + fieldName + "+'%'");
                    }
                    else
                    {
                        builder.Append(" '%'+@" + fieldName + "+'%'");
                    }
                    break;
                case Constants.SqlOperator.NOTNULL:
                    builder.Append(" IS NOT NULL");
                    break;
                case Constants.SqlOperator.ISNULL:
                    builder.Append(" IS NULL");
                    break;
                case Constants.SqlOperator.IN:
                case Constants.SqlOperator.NOTIN:
                    builder.Append("(");
                    if (condition.Conditions.IsNullOrEmpty())
                    {
                        for (int i = 0; i < Values.Count; i++)
                        {
                            builder.Append("@" + fieldName + Convert.ToString(i));
                            valueMap.TryAdd(fieldName + Convert.ToString(i), ConvertUtil.ParseByType(ColumnType, Values[i]));
                            if (i != Values.Count - 1)
                            {
                                builder.Append(",");
                            }
                        }
                    }
                    else
                    {
                        GenerateSubQuery(builder, condition.Conditions, valueMap, duplicatedMap);
                    }
                    builder.Append(")");
                    break;
                case Constants.SqlOperator.EXISTS:
                case Constants.SqlOperator.NOTEXISTS:
                    builder.Append("(");
                    GenerateSubQuery(builder, condition.Conditions, valueMap, duplicatedMap);
                    builder.Append(")");
                    break;
                default:
                    builder.Append("@" + fieldName);
                    valueMap.TryAdd(fieldName, ConvertUtil.ParseByType(ColumnType, Values[0]));
                    break;

            }
        }
        private static void GenerateSubQuery(StringBuilder builder, List<FilterCondition> Conditions, Dictionary<string, object> valueMap, Dictionary<string, int> duplicatedMap)
        {
            if (Conditions.Count == 1)
            {
                FilterCondition condition = Conditions[0];
                builder.Append("SELECT").Append(condition.ColumnName).Append(" FROM ").Append(condition.TargetEntity.GetTableName());
                builder.Append(condition.GeneratePreparedSql(valueMap, duplicatedMap, []));
            }
        }
        public static List<object> WrapParameter(string columnName, Type columnType, Constants.SqlOperator sqlOper, object value)
        {
            List<object> retList = [];
            switch (sqlOper)
            {
                case Constants.SqlOperator.IN:
                case Constants.SqlOperator.NOTIN:
                    string[] arr = value.ToString().Split(',');
                    retList.AddRange(arr.ToList().Select(x => ConvertUtil.ParseByType(columnType, x)).ToList());
                    break;
                case Constants.SqlOperator.BT:
                    string[] arr1 = value.ToString().Split(',');
                    Trace.Assert(arr1.Length >= 2, "");
                    retList.Add(ConvertUtil.ParseByType(columnType, arr1[0]));
                    retList.Add(ConvertUtil.ParseByType(columnType, arr1[1]));
                    break;
                default:
                    retList.Add(ConvertUtil.ParseByType(columnType, value.ToString()));
                    break;
            }
            return retList;
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
        public IList<object> ParamObjects
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
        public IList<object> ParameterObjects
        {
            get; set;
        }
        public bool UpdateRequired
        {
            get; set;
        } = false;
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
