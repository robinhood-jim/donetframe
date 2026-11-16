using Frameset.Core.Common;
using Frameset.Core.Dao.Meta;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Reflect;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Frameset.Core.Dao
{
    public static class DaoUtils
    {
        internal static void AppendCondition(string queryColumn, object queryParam, Dictionary<string, FieldContent> fieldMap, StringBuilder builder, Dictionary<string, int> duplicateMap, Dictionary<string, object> preparedMap)
        {
            if (Constants.IGNOREPARAMS.Contains(queryColumn.ToUpper()))
            {
                return;
            }
            if (string.Equals(Constants.LINK_OR, queryColumn[..2], StringComparison.OrdinalIgnoreCase) || string.Equals(Constants.LINK_AND, queryColumn[..3], StringComparison.OrdinalIgnoreCase))
            {
                string cmpOper = Constants.LINK_AND;
                if (string.Equals(Constants.LINK_OR, queryColumn[..2], StringComparison.OrdinalIgnoreCase))
                {
                    cmpOper = Constants.LINK_OR;
                }
                string? queryStr = queryParam.ToString();
                Trace.Assert(!string.IsNullOrWhiteSpace(queryStr), "");
                Dictionary<string, object>? dict = JsonSerializer.Deserialize<Dictionary<string, object>>(queryStr);
                Trace.Assert(dict != null, "");
                AppendSub(builder, cmpOper, dict, fieldMap, duplicateMap, preparedMap);
            }
            else
            {

                fieldMap.TryGetValue(queryColumn, out FieldContent? content);
                if (content == null)
                {
                    throw new BaseSqlException("property " + queryColumn + " not found in Model");
                }
                Constants.SqlOperator sqloperator = Constants.SqlOperator.EQ;
                FilterCondition condition = new()
                {
                    ColumnName = content.FieldName,
                    ColumnType = content.ParamType
                };
                JsonElement element = (JsonElement)queryParam;
                if (element.ValueKind == JsonValueKind.Object)
                {
                    string? queryStr = queryParam.ToString();
                    Trace.Assert(!string.IsNullOrWhiteSpace(queryStr), "");
                    Dictionary<string, object>? paramMap = JsonSerializer.Deserialize<Dictionary<string, object>>(queryStr);
                    Trace.Assert(paramMap != null, "");
                    paramMap.TryGetValue("operator", out object? operStr);
                    if (operStr != null)
                    {
                        string? cmpStr = operStr.ToString();
                        Trace.Assert(!string.IsNullOrWhiteSpace(cmpStr), "");
                        sqloperator = Constants.Parse(cmpStr);
                    }
                    if (paramMap.TryGetValue("values", out object? values))
                    {
                        condition.Values = SqlUtils.WrapParameter(content.FieldName, content.ParamType, sqloperator, values);
                    }
                }
                else
                {
                    string? queryStr = queryParam.ToString();
                    Trace.Assert(!string.IsNullOrWhiteSpace(queryStr), "");
                    condition.Values = [ConvertUtil.ParseByType(content.ParamType, queryStr)];
                }
                condition.Operator = sqloperator;

                SqlUtils.AppendPreparedSql(condition, preparedMap, builder, duplicateMap, []);
            }
        }
        internal static void AppendSub(StringBuilder builder, string linkOper, Dictionary<string, object> dict, Dictionary<string, FieldContent> fieldMap, Dictionary<string, int> duplicateMap, Dictionary<string, object> preparedMap)
        {
            builder.Append(" (");
            foreach (var entry in dict)
            {
                AppendCondition(entry.Key, entry.Value, fieldMap, builder, duplicateMap, preparedMap);
                builder.Append(' ').Append(linkOper).Append(' ');
            }
            builder.Remove(builder.Length - linkOper.Length - 2, linkOper.Length + 2);
            builder.Append(')');
        }
        internal static DbParameter[] ParseParameter(ISqlDialect dataMeta, object[] obj)
        {
            if (obj.Length <= 0) return [];
            DbParameter[] paramters = new DbParameter[obj.Length];
            for (int i = 0; i < obj.Length; i++)
            {
                paramters[i] = dataMeta.WrapParameter(i, obj[i]);
            }
            return paramters;
        }
        internal static void ParseParameter(ISqlDialect dataMeta, DbCommand command, Dictionary<string, object> paramMap)
        {
            if (paramMap.IsNullOrEmpty()) return;
            foreach (var item in paramMap)
            {
                command.Parameters.Add(dataMeta.WrapParameter(item.Key, item.Value));
            }
        }
        internal static void WrapEntity(Dictionary<string, FieldContent> map, FieldContent? content, string propName, object entity, object value)
        {
            object realValue = value;
            List<string> propList = map.Keys.ToList();
            if (HasColumn(propList, propName, out string? dbColName) && !string.IsNullOrWhiteSpace(dbColName) && map.TryGetValue(dbColName, out content) && content != null)
            {
                if (!Convert.IsDBNull(realValue))
                {
                    content.Setter(entity, ConvertUtil.ParseByType(content.ParamType, realValue));
                }
            }
        }
        internal static List<Dictionary<string, object>> SimpleQuery(DbCommand command)
        {
            List<Dictionary<string, object>> list = [];
            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Dictionary<string, object> dict = [];
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        dict[reader.GetName(col)] = reader[col];
                    }
                    list.Add(dict);
                }
            }
            return list;
        }
        internal static T WrapRow<T>(DbDataReader reader, bool ifRetMap, Dictionary<string, MethodParam>? methodMap, Func<int, string>? columnNameFunc = null)
        {
            T entity = Activator.CreateInstance<T>();
            Trace.Assert(entity != null);
            for (int col = 0; col < reader.FieldCount; col++)
            {
                if (ifRetMap)
                {
                    Dictionary<string, object>? dict = entity as Dictionary<string, object>;
                    Trace.Assert(dict != null);
                    dict[reader.GetName(col)] = reader[col];
                }
                else
                {
                    Trace.Assert(methodMap != null);
                    List<string> columns = methodMap.Keys.ToList();
                    string columnName = reader.GetName(col);

                    if (columnNameFunc != null)
                    {
                        columnName = columnNameFunc.Invoke(col);
                    }
                    if (DaoUtils.HasColumn(columns, columnName, out string? propName) && !string.IsNullOrWhiteSpace(propName) && methodMap.TryGetValue(propName, out MethodParam? param))
                    {
                        if (!Convert.IsDBNull(reader[col]))
                        {
                            param.Setter(entity, ConvertUtil.ParseByType(param.ParamType, reader[col]));
                        }
                    }
                }
            }
            return entity;
        }
        internal static void AppendParameters(DbCommand command, ISqlDialect _dataMeta, Dictionary<string, object> parameterMap)
        {
            if (!parameterMap.IsNullOrEmpty())
            {
                foreach (var item in parameterMap)
                {
                    object value = item.Value;
                    if (value != null)
                    {
                        if (value.GetType().IsFundamentalType())
                        {
                            command.Parameters.Add(_dataMeta.WrapParameter(item.Key, item.Value));
                        }
                        else if (value.GetType().IsTypeDictionary())
                        {
                            Dictionary<string, object>? dict = value as Dictionary<string, object>;
                            Trace.Assert(dict != null, "");
                            foreach (var item1 in dict)
                            {
                                command.Parameters.Add(_dataMeta.WrapParameter(item1.Key, item1.Value));
                            }
                        }
                        else
                        {
                            command.Parameters.Add(_dataMeta.WrapParameter(item.Key, item.Value));
                        }
                    }
                }
            }
        }
        internal static bool IsAnonymousType(Type type) => type.Name.Contains("AnonymousType");
        internal static List<string> GetResultColumns(IDataReader reader)
        {
            var table = reader.GetSchemaTable();
            Trace.Assert(table != null);
            return table.Rows.Cast<DataRow>().Select(row => Convert.ToString(row["ColumnName"])).ToList();
        }
        public static bool HasColumn(List<string> columns, string columnName, out string? dbColName)
        {
            dbColName = columns.Find(x => string.Equals(x, columnName, StringComparison.OrdinalIgnoreCase));
            return dbColName != null;
        }
        internal static List<string> GetParameters(Dictionary<string, MethodParam> dict)
        {
            return dict.Keys.ToList();
        }
    }
}
