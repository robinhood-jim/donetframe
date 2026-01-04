using Frameset.Core.Common;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Mapper;
using Frameset.Core.Mapper.Segment;
using Frameset.Core.Query;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Frameset.Core.Repo
{
    public static class RepositoryHelper
    {
        public static void GetQueryParam(IJdbcDao dao, PageQuery query, Dictionary<string, FieldContent> fieldMap, List<DbParameter> dbParameters, StringBuilder builder)
        {
            int currentParamCount = 0;
            foreach (var item in query.Parameters)
            {
                if (fieldMap.TryGetValue(item.Key, out FieldContent fieldContent) && fieldContent != null && item.Value != null)
                {
                    Constants.SqlOperator oper = Constants.SqlOperator.EQ;

                    List<object> values = [];
                    if (item.Value.ToString().Contains('%'))
                    {
                        if (item.Value.ToString().StartsWith('%'))
                        {
                            oper = Constants.SqlOperator.LLIKE;
                        }
                        else if (item.Value.ToString().EndsWith('%'))
                        {
                            oper = Constants.SqlOperator.RLIKE;
                        }
                        else
                        {
                            oper = Constants.SqlOperator.LIKE;
                        }
                        values.Add(item.Value.ToString().Replace("%", ""));
                    }
                    else if (item.Value.ToString().Contains("|"))
                    {
                        string[] sep = item.Value.ToString().Split('|');
                        oper = Constants.Parse(sep[0]);
                        values.Add(sep[1]);
                    }
                    else
                    {
                        values.Add(item.Value.ToString());
                    }
                    IList<DbParameter> parameters = ParameterHelper.AddQueryParam(dao, fieldContent, builder, currentParamCount, out int addCount, oper, values.ToArray());
                    if (!parameters.IsNullOrEmpty())
                    {
                        dbParameters.AddRange(parameters);
                        builder.Append(" AND ");
                    }
                    currentParamCount += addCount;
                }
            }
            if (builder.ToString().EndsWith(" AND "))
            {
                builder.Remove(builder.Length - 5, 5);
            }
            if (!query.Order.IsNullOrEmpty())
            {
                builder.Append(" ORDER BY ").Append(query.Order);
            }
            else if (!query.OrderField.IsNullOrEmpty())
            {
                fieldMap.TryGetValue(query.OrderField, out FieldContent fieldContent);
                if (fieldContent == null)
                {
                    throw new OperationFailedException("orderField " + query.OrderField + " not in table!");
                }
                builder.Append(" ORDER BY ").Append(fieldContent.FieldName).Append(query.OrderAsc ? " ASC" : " DESC");
            }
        }
        public static void ExecuteMapperBefore(IJdbcDao dao, AbstractSegment segment, string nameSpace, object input, out StringBuilder builder,
            out Dictionary<string, object> paramMap, out bool retMap, out bool returnInsert, out string generateKey, out Dictionary<string, MethodParam> methodMap)
        {
            paramMap = new Dictionary<string, object>();
            CompositeSegment csegment = (CompositeSegment)segment;
            string rsMap = csegment.Parametertype;
            Type retType = null;
            if (!csegment.Parametertype.IsNullOrEmpty())
            {
                ConvertUtil.ToDict(input, paramMap);
            }
            else if (input.GetType().Equals(typeof(Dictionary<string, object>)))
            {
                paramMap = (Dictionary<string, object>)input;
            }

            ResultMap map = SqlMapperConfigure.GetResultMap(nameSpace, rsMap);
            retType = map != null ? map.ModelType : Type.GetType(rsMap);

            methodMap = null;
            retMap = false;
            returnInsert = false;
            generateKey = string.Empty;
            if (retType == null && string.Equals(rsMap, "Map", StringComparison.OrdinalIgnoreCase))
            {
                retMap = true;
            }
            else
            {
                methodMap = AnnotationUtils.ReflectObject(retType);
            }

            builder = new(segment.ReturnSqlPart(paramMap));

            if (csegment.GetType().Equals(typeof(SqlInsertSegment)))
            {
                SqlInsertSegment insertSegment = (SqlInsertSegment)segment;

                if (insertSegment.UseGenerateKey)
                {
                    builder.Append(dao.GetDialect().AppendKeyHolder());
                    returnInsert = true;
                }
                else if (!insertSegment.SequenceName.IsNullOrEmpty())
                {
                    builder.Append(dao.GetDialect().AppendSequence(insertSegment.SequenceName));
                    returnInsert = true;
                }
                generateKey = insertSegment.KeyProperty;

            }
            Trace.Assert(!paramMap.IsNullOrEmpty(), "all property is null");
        }
        public static int ExecuteInTransaction(IJdbcDao dao, string sql, Func<DbCommand, int> func)
        {
            using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
            {
                connection.Open();
                DbTransaction transaction = connection.BeginTransaction();
                try
                {
                    DbCommand command = dao.GetDialect().GetDbCommand(connection, sql);
                    command.Transaction = transaction;
                    int ret = func.Invoke(command);
                    transaction.Commit();
                    return ret;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new BaseSqlException(ex.Message);
                }
            }
        }
        public static Tuple<StringBuilder, IList<DbParameter>> QueryModelByFieldBefore(Type entityType, IJdbcDao dao, IList<FieldContent> fields, string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null)
        {
            FieldContent fielContent = fields.First(x => string.Equals(x.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase));
            if (fielContent == null)
            {
                fielContent = fields.First(x => string.Equals(x.FieldName, propertyName, StringComparison.OrdinalIgnoreCase));
            }
            if (fielContent == null)
            {
                throw new BaseSqlException("propertyName " + propertyName + " not found in Model");
            }
            StringBuilder builder = new StringBuilder(SqlUtils.GetSelectSql(entityType)).Append(" WHERE ");

            IList<DbParameter> parameters = ParameterHelper.AddQueryParam(dao, fielContent, builder, 0, out int endPos, oper, values);
            if (!orderByStr.IsNullOrEmpty())
            {
                builder.Append(" order by ").Append(orderByStr);
            }
            return Tuple.Create(builder, parameters);
        }

        public static T ExecuteInTransaction<V, T>(IJdbcDao dao, string sql, V entity, Func<DbCommand, V, T> func)
        {
            using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
            {
                connection.Open();
                DbTransaction transaction = connection.BeginTransaction();
                try
                {
                    DbCommand command = dao.GetDialect().GetDbCommand(connection, sql);
                    command.Transaction = transaction;
                    T ret = func.Invoke(command, entity);
                    transaction.Commit();
                    return ret;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new BaseSqlException(ex.Message);
                }
                finally
                {
                    transaction.Dispose();
                }
            }
        }
        public static int ExecuteInTransaction(IJdbcDao dao, Func<DbConnection, int> func)
        {
            using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
            {
                connection.Open();
                DbTransaction transaction = connection.BeginTransaction();
                try
                {
                    int ret = func.Invoke(connection);
                    transaction.Commit();
                    return ret;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new BaseSqlException(ex.Message);
                }
                finally
                {
                    transaction.Dispose();
                }
            }
        }
    }

}
