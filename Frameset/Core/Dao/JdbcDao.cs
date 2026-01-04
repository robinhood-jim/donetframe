using Frameset.Core.Common;
using Frameset.Core.Dao.Meta;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Mapper;
using Frameset.Core.Mapper.Segment;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;
using Serilog;
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
    public class JdbcDao : IJdbcDao
    {
        private readonly string connectionStr;
        private readonly string dbTypeStr = "Mysql";
        private readonly AbstractSqlDialect dataMeta;
        private readonly Constants.DbType dbType;
        private readonly string schema;

        internal JdbcDao(string connectionStr)
        {
            this.connectionStr = connectionStr;

        }
        internal JdbcDao(string dbTypeStr, string connectionStr)
        {
            this.dbTypeStr = dbTypeStr;
            this.connectionStr = connectionStr;
            this.dbType = Constants.DbTypeOf(dbTypeStr);
            this.dataMeta = DbDialectFactory.GetInstance(dbType);
        }
        internal JdbcDao(string dbTypeStr, string schema, string connectionStr)
        {
            this.dbTypeStr = dbTypeStr;
            this.connectionStr = connectionStr;
            this.dbType = Constants.DbTypeOf(dbTypeStr);
            this.dataMeta = DbDialectFactory.GetInstance(dbType);
            this.schema = schema;
        }
        public int QueryByInt(DbCommand command, List<DbParameter> parameters = null)
        {
            try
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters.ToArray());
                }
                return (int)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new BaseSqlException("", ex);
            }
        }

        public long QueryByLong(DbCommand command, List<DbParameter> parameters = null)
        {
            try
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters.ToArray());
                }
                return (long)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new BaseSqlException("", ex);
            }
        }



        public IList<V> QueryModelsBySql<V>(Type modelType, DbCommand command, IList<DbParameter> parameters = null)
        {
            Trace.Assert(modelType.IsSubclassOf(typeof(BaseEntity)));
            if (!parameters.IsNullOrEmpty())
            {
                command.Parameters.AddRange(parameters.ToArray());
            }
            IList<V> retList = new List<V>();
            Dictionary<string, FieldContent> map = EntityReflectUtils.GetFieldsMap(modelType);
            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    dynamic entity = System.Activator.CreateInstance(modelType);
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        string propName = reader.GetName(col);
                        object value = reader[col];
                        FieldContent content = map[propName];
                        if (Convert.IsDBNull(value))
                        {
                            map.TryGetValue(propName.ToLower(), out content);
                        }
                        if (Convert.IsDBNull(value))
                        {
                            map.TryGetValue(propName.ToUpper(), out content);
                        }
                        if (!Convert.IsDBNull(value) && content != null)
                        {
                            content.SetMethod.Invoke(entity, new object[] { ConvertUtil.ParseByType(content.ParamType, value) });
                        }
                    }
                    retList.Add(entity);
                }
            }
            return retList;

        }
        public IList<BaseEntity> QueryModelsBySql(Type modelType, DbCommand command, IList<DbParameter> parameters)
        {
            Trace.Assert(modelType.IsSubclassOf(typeof(BaseEntity)));
            command.Parameters.AddRange(parameters.ToArray());
            IList<BaseEntity> retList = new List<BaseEntity>();
            Dictionary<string, FieldContent> map = EntityReflectUtils.GetFieldsMap(modelType);
            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    dynamic entity = Activator.CreateInstance(modelType);
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        string propName = reader.GetName(col);
                        object value = reader[col];
                        FieldContent content = map[propName];
                        if (Convert.IsDBNull(value))
                        {
                            map.TryGetValue(propName.ToLower(), out content);
                        }
                        if (Convert.IsDBNull(value))
                        {
                            map.TryGetValue(propName.ToUpper(), out content);
                        }
                        if (!Convert.IsDBNull(value) && content != null)
                        {
                            content.SetMethod.Invoke(entity, new object[] { ConvertUtil.ParseByType(content.ParamType, value) });
                        }
                    }
                    retList.Add(entity);
                }
            }
            return retList;

        }
        public IList<Dictionary<string, object>> QueryBySql(DbCommand command, object[] objects)
        {
            DbParameter[] parameters = ParseParameter(objects);
            command.Parameters.AddRange(parameters);
            IList<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            using (DbDataReader reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        dict[reader.GetName(col)] = reader[col];
                    }
                    list.Add(dict);
                }

            }
            return list;
        }
        public List<O> QueryByNamedParameter<O>(DbCommand command, Dictionary<string, object> namedParamter)
        {
            ParseParameter(command, namedParamter);
            List<O> retList = new();
            bool retMap = typeof(O).Equals(typeof(Dictionary<string, object>));
            Dictionary<string, MethodParam> methodMap = [];
            if (!retMap)
            {
                methodMap = AnnotationUtils.ReflectObject(typeof(O));
            }
            using (DbDataReader reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    dynamic retObj = Activator.CreateInstance<O>();
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        if (retMap)
                        {
                            (retObj as Dictionary<string, object>)[reader.GetName(col)] = reader[col];
                        }
                        else
                        {
                            if (methodMap.TryGetValue(reader.GetName(col), out MethodParam param))
                            {
                                param.SetMethod.Invoke(retObj, new object[] { ConvertUtil.ParseByType(param.ParamType, reader[col]) });
                            }
                        }

                    }
                    retList.Add(retObj);
                }
            }
            return retList;
        }
        private DbParameter[] ParseParameter(object[] obj)
        {
            if (obj != null && obj.Length > 0)
            {
                DbParameter[] paramters = new DbParameter[obj.Length];
                for (int i = 0; i < obj.Length; i++)
                {
                    paramters[i] = dataMeta.WrapParameter(i, obj[i]);
                }
                return paramters;
            }
            return [];
        }
        void ParseParameter(DbCommand command, Dictionary<string, object> paramMap)
        {
            if (!paramMap.IsNullOrEmpty())
            {
                foreach (var item in paramMap)
                {
                    command.Parameters.Add(dataMeta.WrapParameter(item.Key, item.Value));
                }
            }
        }


        public PageDTO<V> QueryPage<V>(DbCommand command, PageQuery query)
        {
            ParseParameter(command, query.Parameters);
            string querySql = null;
            string countSql = null;
            ResultMap mappingMap = null;

            if (!query.QueryId.IsNullOrEmpty())
            {
                SqlSelectSegment segment = (SqlSelectSegment)SqlMapperConfigure.GetExecuteSegment(query.NameSpace, query.QueryId);
                querySql = segment.ReturnSqlPart(query.Parameters);
                mappingMap = SqlMapperConfigure.GetResultMap(query.NameSpace, segment.ResultMap);
            }
            else
            {
                querySql = query.QuerySql;
            }
            countSql = dataMeta.GenerateCountSql(querySql);
            command.CommandText = countSql;
            long count = QueryByLong(command);
            PageDTO<V> ret = new PageDTO<V>(count, query.PageSize, query.CurrentPage);
            string pageSql = dataMeta.GeneratePageSql(querySql, query);
            Type retType = typeof(V);
            bool ifRetMap = false;
            Dictionary<string, MethodParam> methodMap = null;

            if (retType.Equals(typeof(Dictionary<string, object>)))
            {
                ifRetMap = true;
            }
            else
            {
                methodMap = AnnotationUtils.ReflectObject(retType);
            }
            command.CommandText = pageSql;
            using (DbDataReader reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    V entity = System.Activator.CreateInstance<V>();
                    Dictionary<string, object> dict = null;
                    if (ifRetMap)
                    {
                        dict = new Dictionary<string, object>();
                    }

                    MethodParam param = null;
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        string name = reader.GetName(col);
                        if (ifRetMap)
                        {
                            dict[reader.GetName(col)] = reader[col];
                        }
                        string mappingColumn = null;

                        if ((mappingMap == null || !mappingMap.MappingColumns.TryGetValue(name, out mappingColumn)) && !query.MappingColumns.TryGetValue(name, out mappingColumn))
                        {
                            mappingColumn = Core.Utils.StringUtils.CamelCaseLowConvert(name);
                        }

                        if (methodMap.TryGetValue(mappingColumn, out param))
                        {
                            param.SetMethod.Invoke(entity, new object[] { ConvertUtil.ParseByType(param.ParamType, reader[col]) });
                        }
                    }
                    if (ifRetMap)
                    {
                        dynamic tmp = Convert.ChangeType(dict, retType);
                        ret.Results.Add(tmp);
                    }
                    else
                    {
                        ret.Results.Add(entity);
                    }
                }
            }
            return ret;

        }


        public bool SaveEntity(DbCommand command, BaseEntity model, InsertSegment segment)
        {
            command.Parameters.AddRange(segment.Parameters.ToArray());
            command.CommandText = segment.InsertSql;
            if (segment.Increment || segment.Sequence)
            {
                object genId = command.ExecuteScalar();
                if (genId != null)
                {
                    segment.GenKeyMethod.Invoke(model, new object[] { ConvertUtil.ParseByType(segment.GenKeyMethod.GetParameters()[0].ParameterType, genId) });
                }
            }
            else
            {
                command.ExecuteNonQuery();
            }
            return true;

        }
        public bool UpdateEntity(DbCommand command, UpdateSegment segment)
        {
            command.CommandText = segment.UpdateSql;
            command.Parameters.AddRange(segment.Parameters.ToArray());
            return command.ExecuteNonQuery() == 1;
        }
        public int Execute(DbCommand command, string sql, DbParameter[] parameters)
        {
            command.Parameters.AddRange(parameters);
            command.CommandText = sql;
            return command.ExecuteNonQuery();
        }
        public List<V> QueryByConditon<V>(DbCommand command, FilterCondition condition)
        {
            Dictionary<string, object> queryParamter = [];
            StringBuilder builder = new StringBuilder();
            if (!condition.SelectParts.IsNullOrEmpty())
            {
                builder.Append(condition.SelectParts);
            }
            else
            {
                builder.Append(SqlUtils.GetSelectSql(typeof(V)));
            }
            builder.Append(condition.GeneratePreparedSql(queryParamter, [], []));
            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                Log.Debug("using Query {Query}", builder.ToString());
            }
            string querySql = builder.ToString();
            ParseParameter(command, queryParamter);
            command.CommandText = querySql;
            bool ifRetMap = false;
            Dictionary<string, MethodParam> methodMap = null;

            Type retType = typeof(V);
            var retList = new List<V>();
            if (retType.Equals(typeof(Dictionary<string, object>)))
            {
                ifRetMap = true;
            }
            else
            {
                methodMap = AnnotationUtils.ReflectObject(retType);
            }
            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    V entity = Activator.CreateInstance<V>();
                    MethodParam param = null;
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        if (ifRetMap)
                        {
                            (entity as Dictionary<string, object>)[reader.GetName(col)] = reader[col];
                        }
                        else
                        {
                            if (methodMap.TryGetValue(reader.GetName(col), out param))
                            {
                                param.SetMethod.Invoke(entity, new object[] { ConvertUtil.ParseByType(param.ParamType, reader[col]) });
                            }
                        }
                    }
                    retList.Add(entity);
                }
            }
            return retList;
        }
        public List<O> QueryByFields<O>(Type entityType, DbCommand command, QueryParameter queryParams)
        {
            StringBuilder builder = new();
            string selectPart = null;
            StringBuilder newColumnsBuilder = new();
            StringBuilder havingBuiler = new();
            StringBuilder groupByBuilder = new();
            string orderByStr;
            Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(entityType);
            Dictionary<string, string> propFieldMap = [];

            Dictionary<string, int> duplicatedMap = [];
            StringBuilder whereBuilder = new();
            Dictionary<string, object> preparedMap = [];
            if (!queryParams.SelectColumns.IsNullOrEmpty())
            {
                StringBuilder originBuilder = new(Constants.SQL_SELECT);
                foreach (var columnName in queryParams.SelectColumns.Split(','))
                {
                    fieldMap.TryGetValue(columnName, out FieldContent oContent);
                    Trace.Assert(oContent != null, "property " + columnName + " not defined in Model " + entityType.Name);
                    originBuilder.Append(oContent.FieldName).Append(Constants.SQL_AS).Append(oContent.PropertyName).Append(",");
                }
                originBuilder.Remove(originBuilder.Length - 1, 1);
                selectPart = originBuilder.ToString();
            }
            if (!queryParams.NewColumns.IsNullOrEmpty())
            {
                foreach (var newEntry in queryParams.NewColumns)
                {
                    string asColumnName = newEntry.Key;
                    List<string> columns = JsonSerializer.Deserialize<List<string>>(newEntry.Value.ToString());
                    StringBuilder arithBuilder = new();
                    bool containFunc = false;
                    foreach (var selPart in columns)
                    {
                        if (Constants.SQLFUNCTIONS.Contains(selPart.ToUpper()))
                        {
                            containFunc = true;
                            arithBuilder.Append(selPart).Append('(');
                        }
                        else
                        {
                            fieldMap.TryGetValue(selPart, out FieldContent fieldContent);
                            if (fieldContent != null)
                            {
                                arithBuilder.Append(fieldContent.FieldName);
                            }
                            else
                            {
                                arithBuilder.Append(selPart);
                            }
                        }
                    }
                    if (containFunc)
                    {
                        arithBuilder.Append(')');
                    }
                    newColumnsBuilder.Append(arithBuilder).Append(Constants.SQL_AS).Append(asColumnName);
                    propFieldMap.TryAdd(asColumnName, arithBuilder.ToString());
                }
            }
            if (!queryParams.GroupBy.IsNullOrEmpty())
            {
                foreach (var columnName in queryParams.GroupBy.Split(','))
                {
                    fieldMap.TryGetValue(columnName, out FieldContent oContent);
                    Trace.Assert(oContent != null, "property " + columnName + " not defined in Model " + entityType.Name);
                    groupByBuilder.Append(oContent.FieldName).Append(",");
                }
            }
            if (!queryParams.Having.IsNullOrEmpty())
            {
                foreach (var havingEntry in queryParams.Having)
                {
                    string columnName = havingEntry.Key;
                    propFieldMap.TryGetValue(columnName, out string funcStr);
                    Trace.Assert(!funcStr.IsNullOrEmpty(), "having column alias not defined!");
                    if (havingEntry.Value is JsonElement)
                    {
                        Dictionary<string, object> dict1 = JsonSerializer.Deserialize<Dictionary<string, object>>(havingEntry.Value.ToString());
                        Constants.SqlOperator compareOper = Constants.Parse(dict1["operator"].ToString());
                        double cmpValue = Convert.ToDouble(dict1["values"].ToString());

                        havingBuiler.Append(funcStr).Append(Constants.OperatorValue(compareOper)).Append('@').Append(columnName).Append(Constants.SQL_AND);
                        preparedMap.TryAdd(columnName, cmpValue);
                    }
                    else
                    {
                        havingBuiler.Append(funcStr).Append('=').Append('@').Append(columnName).Append(Constants.SQL_AND);
                        preparedMap.TryAdd(columnName, Convert.ToDouble(havingEntry.Value.ToString()));
                    }
                }
            }
            orderByStr = queryParams.OrderBy;

            foreach (var entry in queryParams.Parameters)
            {
                AppendCondtion(entry.Key, entry.Value, fieldMap, whereBuilder, duplicatedMap, preparedMap);
                if (whereBuilder.Length > 0)
                {
                    whereBuilder.Append(Constants.SQL_AND);
                }
            }
            if (selectPart.IsNullOrEmpty())
            {
                builder.Append(SqlUtils.GetSelectSql(entityType)).Append(Constants.SQL_WHERE);
            }
            else
            {
                EntityContent entityContent = EntityReflectUtils.GetEntityInfo(entityType);
                builder.Append(selectPart);
                if (newColumnsBuilder.Length > 0)
                {
                    builder.Append(',').Append(newColumnsBuilder);
                }
                builder.Append(" FROM ").Append(entityContent.GetTableName()).Append(Constants.SQL_WHERE);
            }
            whereBuilder.Remove(whereBuilder.Length - Constants.SQL_AND.Length, Constants.SQL_AND.Length);
            builder.Append(whereBuilder);
            if (groupByBuilder.Length > 0)
            {
                groupByBuilder.Remove(groupByBuilder.Length - 1, 1);
                builder.Append(Constants.SQL_GROUPBY).Append(groupByBuilder);
            }
            if (havingBuiler.Length > 0)
            {
                havingBuiler.Remove(havingBuiler.Length - Constants.SQL_AND.Length, Constants.SQL_AND.Length);
                builder.Append(Constants.SQL_HAVING).Append(havingBuiler);
            }
            if (!orderByStr.IsNullOrEmpty())
            {
                builder.Append(Constants.SQL_ORDERBY).Append(orderByStr);
            }
            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                Log.Debug("query Sql={Builder}", builder.ToString());
            }
            command.CommandText = builder.ToString();
            bool ifRetMap = false;
            var retList = new List<O>();
            Type retType = typeof(O);
            Dictionary<string, MethodParam> methodMap = null;
            if (retType.Equals(typeof(Dictionary<string, object>)))
            {
                ifRetMap = true;
            }
            else
            {
                methodMap = AnnotationUtils.ReflectObject(retType);
            }
            ParseParameter(command, preparedMap);
            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    O entity = Activator.CreateInstance<O>();
                    MethodParam param = null;
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        if (ifRetMap)
                        {
                            (entity as Dictionary<string, object>)[reader.GetName(col)] = reader[col];
                        }
                        else
                        {
                            if (methodMap.TryGetValue(reader.GetName(col), out param))
                            {
                                param.SetMethod.Invoke(entity, [ConvertUtil.ParseByType(param.ParamType, reader[col])]);
                            }
                        }
                    }
                    retList.Add(entity);
                }
            }
            return retList;

        }
        private void AppendCondtion(string queryColumn, object queryParam, Dictionary<string, FieldContent> fieldMap, StringBuilder builder, Dictionary<string, int> duplicateMap, Dictionary<string, object> preparedMap)
        {
            if (Constants.IGNOREPARAMS.Contains(queryColumn.ToUpper()))
            {
                return;
            }
            else if (string.Equals(Constants.LINK_OR, queryColumn.Substring(0, 2), StringComparison.OrdinalIgnoreCase) || string.Equals(Constants.LINK_AND, queryColumn.Substring(0, 3), StringComparison.OrdinalIgnoreCase))
            {
                string cmpOper = Constants.LINK_AND;
                if (string.Equals(Constants.LINK_OR, queryColumn.Substring(0, 2), StringComparison.OrdinalIgnoreCase))
                {
                    cmpOper = Constants.LINK_OR;
                }
                Dictionary<string, object> dict = JsonSerializer.Deserialize<Dictionary<string, object>>(queryParam.ToString());
                AppendSub(builder, cmpOper, dict, fieldMap, duplicateMap, preparedMap);
            }
            else
            {

                fieldMap.TryGetValue(queryColumn, out FieldContent content);
                if (content == null)
                {
                    throw new BaseSqlException("property " + queryColumn + " not found in Model");
                }
                Constants.SqlOperator sqloperator = Constants.SqlOperator.EQ;
                FilterCondition condition = new FilterCondition()
                {
                    ColumnName = content.FieldName,
                    ColumnType = content.GetMethod.ReturnType
                };
                JsonElement element = (JsonElement)queryParam;
                if (element.ValueKind == JsonValueKind.Object)
                {
                    Dictionary<string, object> paramMap = JsonSerializer.Deserialize<Dictionary<string, object>>(queryParam.ToString());
                    paramMap.TryGetValue("operator", out object operStr);
                    if (operStr != null)
                    {
                        sqloperator = Constants.Parse(operStr.ToString());
                    }
                    paramMap.TryGetValue("values", out object values);
                    condition.Values = SqlUtils.WrapParameter(content.FieldName, content.ParamType, sqloperator, values);
                }
                else
                {
                    condition.Values = [ConvertUtil.ParseByType(content.ParamType, queryParam.ToString())];
                }
                condition.Operator = sqloperator;

                SqlUtils.AppendPreparedSql(condition, preparedMap, builder, duplicateMap, []);
            }
        }
        private void AppendSub(StringBuilder builder, string linkOper, Dictionary<string, object> dict, Dictionary<string, FieldContent> fieldMap, Dictionary<string, int> duplicateMap, Dictionary<string, object> preparedMap)
        {
            builder.Append(" (");
            foreach (var entry in dict)
            {
                AppendCondtion(entry.Key, entry.Value, fieldMap, builder, duplicateMap, preparedMap);
                builder.Append(" ").Append(linkOper).Append(" ");
            }
            builder.Remove(builder.Length - linkOper.Length - 2, linkOper.Length + 2);
            builder.Append(")");
        }
        public object QueryMapper(SqlSelectSegment sqlsegment, Dictionary<string, object> paramMap, string nameSpace, DbCommand command, object queryObject)
        {
            string rsMap = sqlsegment.ResultMap;
            ResultMap map = SqlMapperConfigure.GetResultMap(nameSpace, rsMap);
            Type retType = map != null ? map.ModelType : Type.GetType(rsMap);
            bool retMap = false;
            Dictionary<string, MethodParam> methodMap = null;
            if (retType == null && string.Equals(rsMap, "Map", StringComparison.OrdinalIgnoreCase))
            {
                retMap = true;
                retType = typeof(Dictionary<string, object>);
            }
            else
            {
                methodMap = AnnotationUtils.ReflectObject(retType);
            }


            if (!paramMap.IsNullOrEmpty())
            {
                foreach (var item in paramMap)
                {
                    command.Parameters.Add(dataMeta.WrapParameter(item.Key, item.Value));
                }
            }
            var listType = typeof(List<>).MakeGenericType(retType);

            dynamic retList = Activator.CreateInstance(listType);

            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Dictionary<string, object> dict = null;
                    object ret = null;
                    MethodParam param = null;

                    if (retMap)
                    {
                        dict = new Dictionary<string, object>();
                    }
                    else
                    {
                        ret = Activator.CreateInstance(retType);
                    }

                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        string name = reader.GetName(col);
                        if (retMap)
                        {
                            dict[reader.GetName(col)] = reader[col];
                        }
                        else if (methodMap.TryGetValue(map.MappingColumns[name], out param))
                        {
                            param.SetMethod.Invoke(ret, new object[] { ConvertUtil.ParseByType(param.ParamType, reader[col]) });
                        }
                    }
                    if (retMap)
                    {
                        retList.Add(dict);
                    }
                    else
                    {
                        dynamic tmp = Convert.ChangeType(ret, retType);
                        retList.Add(tmp);
                    }
                }

            }
            return retList;
        }
        public int ExecuteMapper(CompositeSegment segment, string nameSpace, string executeId, DbCommand command, Dictionary<string, object> paramMap)
        {

            return command.ExecuteNonQuery();

        }
        public void DoWithQuery(string sql, object[] obj, Action<IDataReader> action)
        {
            using (DbConnection connection = dataMeta.GetDbConnection(connectionStr))
            {
                connection.Open();
                using (DbCommand command = dataMeta.GetDbCommand(connection, sql))
                {
                    DbParameter[] parameters = ParseParameter(obj);
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        action.Invoke(reader);
                    }
                }
            }
        }
        public void DoWithQueryNamed(string sql, Dictionary<string, object> QueryParameters, Action<IDataReader> action)
        {

            using (DbConnection connection = dataMeta.GetDbConnection(connectionStr))
            {
                connection.Open();
                using (DbCommand command = dataMeta.GetDbCommand(connection, sql))
                {
                    ParseParameter(command, QueryParameters);
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        action.Invoke(reader);
                    }
                }
            }
        }

        public string GetConnectString()
        {
            return connectionStr;
        }

        public string GetDbTypeStr()
        {
            return dbTypeStr;
        }
        public AbstractSqlDialect GetDialect()
        {
            return dataMeta;
        }
        public string GetCurrentSchema()
        {
            return schema;
        }
    }
}