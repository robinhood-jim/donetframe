using Frameset.Core.Common;
using Frameset.Core.Dao.Meta;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Linq;
using Frameset.Core.Mapper;
using Frameset.Core.Mapper.Segment;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Core.Reflect;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;


namespace Frameset.Core.Dao
{
    public class AdoDao : IAdoDao
    {
        private readonly string _connectionStr;
        private readonly string _dbTypeStr = "Mysql";
        private readonly ISqlDialect _dataMeta = null!;
        private readonly Constants.DbType _dbType = Constants.DbType.Mysql;
        private readonly string _schema = null!;
        private string _logicColumn = string.Empty;
        private int _validValue = Constants.VALID_INT;
        private int _invalidValue = Constants.INVALID_INT;
        #region Construct
        private AdoDao(string connectionStr)
        {
            this._connectionStr = connectionStr;
        }
        internal AdoDao(string dbTypeStr, string connectionStr) : this(connectionStr)
        {
            this._dbTypeStr = dbTypeStr;
            _dbType = Constants.DbTypeOf(dbTypeStr);
            _dataMeta = DbDialectFactory.GetInstance(_dbType);
        }
        internal AdoDao(string dbTypeStr, string schema, string connectionStr) : this(dbTypeStr, connectionStr)
        {
            this._schema = schema;
        }
        internal AdoDao(string dbTypeStr, string schema, string connectionStr, string logicColumn, object? validValue, object? invalidValue) : this(dbTypeStr, schema, connectionStr)
        {
            this._logicColumn = logicColumn;
            if (validValue != null)
            {
                this._validValue = Convert.ToInt32(validValue);
            }
            if (invalidValue != null)
            {
                this._invalidValue = Convert.ToInt32(invalidValue);
            }
        }

        internal AdoDao(ISqlDialect sqlDialect, string connectionStr)
        {
            _dbType = sqlDialect.GetDbType();
            this._connectionStr = connectionStr;
            _dbTypeStr = _dbType.ToString();
            _dataMeta = sqlDialect;
        }
        internal AdoDao(ISqlDialect sqlDialect, string schema, string connectionStr) : this(sqlDialect, connectionStr)
        {
            this._schema = schema;
        }

        internal AdoDao(ISqlDialect sqlDialect, string schema, string connectionStr, string logicColumn, object? validValue, object? invalidValue) : this(sqlDialect, schema, connectionStr)
        {
            this._logicColumn = logicColumn;
            if (validValue != null)
            {
                _validValue = Convert.ToInt32(validValue);
            }
            if (invalidValue != null)
            {
                _invalidValue = Convert.ToInt32(invalidValue);
            }
        }
        public void SetLogicColumn(string logicColumn)
        {
            _logicColumn = logicColumn;

        }
        public string GetLogicColumn()
        {
            return _logicColumn;
        }
        public void SetValidValue(int value)
        {
            _validValue = value;
        }
        public int GetValidValue()
        {
            return _validValue;
        }
        public void SetInvalidValue(int value)
        {
            _invalidValue = value;
        }
        public int GetInvalidValue()
        {
            return _invalidValue;
        }
        #endregion
        #region Query
        public int QueryByInt(DbCommand command, List<DbParameter>? parameters = null)
        {
            try
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters.ToArray());
                }
                object? ret = command.ExecuteScalar();
                return ret == null ? -1 : (int)ret;
            }
            catch (Exception ex)
            {
                throw new BaseSqlException(ex.Message, ex);
            }
        }

        public long QueryByLong(DbCommand command, List<DbParameter>? parameters = null)
        {
            try
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters.ToArray());
                }
                object? ret = command.ExecuteScalar();
                return ret == null ? -1 : (long)ret;
            }
            catch (Exception ex)
            {
                throw new BaseSqlException(ex.Message, ex);
            }
        }

        public List<V> QueryModelsBySql<V>(DbCommand command, List<DbParameter> parameters)
        {
            Type modelType = typeof(V);
            Trace.Assert(modelType.IsSubclassOf(typeof(BaseEntity)));
            if (!parameters.IsNullOrEmpty())
            {
                command.Parameters.AddRange(parameters.ToArray());
            }
            List<V> retList = [];
            Dictionary<string, FieldContent> map = EntityReflectUtils.GetFieldsMap(modelType);
            using DbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                dynamic? entity = Activator.CreateInstance<V>();
                for (int col = 0; col < reader.FieldCount; col++)
                {
                    string propName = reader.GetName(col);
                    object value = reader[col];
                    FieldContent content = map[propName];
                    DaoUtils.WrapEntity(map, content, propName, entity, value);
                }
                retList.Add(entity);
            }

            return retList;
        }
        public List<BaseEntity> QueryModelsBySql(Type modelType, DbCommand command, List<DbParameter> parameters)
        {
            Trace.Assert(modelType.IsSubclassOf(typeof(BaseEntity)));
            if (!parameters.IsNullOrEmpty())
            {
                command.Parameters.AddRange(parameters.ToArray());
            }
            List<BaseEntity> retList = [];
            Dictionary<string, FieldContent> map = EntityReflectUtils.GetFieldsMap(modelType);
            using DbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                dynamic? entity = Activator.CreateInstance(modelType);
                for (int col = 0; col < reader.FieldCount; col++)
                {
                    string propName = reader.GetName(col);
                    object value = reader[col];
                    FieldContent content = map[propName];
                    DaoUtils.WrapEntity(map, content, propName, entity, value);
                }
                retList.Add(entity);
            }

            return retList;
        }

        public List<Dictionary<string, object>> QueryBySql(DbCommand command, object[] objects)
        {
            DbParameter[] parameters = DaoUtils.ParseParameter(_dataMeta, objects);
            command.Parameters.AddRange(parameters);
            return DaoUtils.SimpleQuery(command);
        }

        public object QueryBySql(DbCommand command, Type targetType, string querySql, List<DbParameter>? parameters)
        {
            try
            {
                if (parameters != null && !parameters.IsNullOrEmpty())
                {
                    command.Parameters.AddRange(parameters.ToArray());
                }
                if (targetType.IsTypeDictionary())
                {
                    return DaoUtils.SimpleQuery(command);
                }
                var listType = typeof(List<>).MakeGenericType(targetType);
                var list = Activator.CreateInstance(listType);
                var addMethod = listType.GetMethod("Add");
                Trace.Assert(addMethod != null);

                using var reader = command.ExecuteReader();
                List<string> columns = DaoUtils.GetResultColumns(reader);
                while (reader.Read())
                {
                    object? instance;
                    if (DaoUtils.IsAnonymousType(targetType))
                    {
                        var constructor = targetType.GetConstructors().First();
                        var parameters1 = constructor.GetParameters();
                        var constructorArgs = new object?[parameters1.Length];
                        for (int i = 0; i < parameters1.Length; i++)
                        {
                            string? columnName = parameters1[i].Name;
                            Trace.Assert(!string.IsNullOrWhiteSpace(columnName));
                            if (DaoUtils.HasColumn(columns, columnName, out string? dbColName) && !string.IsNullOrWhiteSpace((dbColName)))
                            {
                                var val = reader[dbColName];
                                constructorArgs[i] = val == DBNull.Value ? null : ConvertUtil.ParseByType(parameters1[i].ParameterType, val);
                            }
                        }
                        instance = constructor.Invoke(constructorArgs);
                    }
                    else
                    {
                        Dictionary<string, MethodParam> methodDict = AnnotationUtils.ReflectObject(targetType);
                        instance = Activator.CreateInstance(targetType);
                        Trace.Assert(instance != null);
                        foreach (var prop in methodDict)
                        {
                            if (DaoUtils.HasColumn(columns, prop.Key, out string? dbColName) && !string.IsNullOrWhiteSpace(dbColName))
                            {
                                var val = reader[dbColName];
                                if (val != DBNull.Value)
                                {
                                    prop.Value.Setter(instance, ConvertUtil.ParseByType(prop.Value.PropertyInfo.PropertyType, val));
                                }
                            }
                        }
                    }
                    addMethod.Invoke(list, [instance]);
                }
                Trace.Assert(list != null);
                return list;
            }
            catch (Exception ex)
            {
                throw new BaseSqlException(ex.Message, ex);
            }
        }

        public List<O> QueryByNamedParameter<O>(DbCommand command, Dictionary<string, object> namedParamter)
        {
            DaoUtils.ParseParameter(_dataMeta, command, namedParamter);
            List<O> retList = [];
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
                    retList.Add(DaoUtils.WrapRow<O>(reader, retMap, methodMap));
                }
            }
            return retList;
        }
        public PageDTO<V> QueryPage<V>(DbCommand command, PageQuery query)
        {
            DaoUtils.ParseParameter(_dataMeta, command, query.Parameters);
            string querySql;
            string countSql;
            ResultMap? mappingMap = null;
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
            countSql = _dataMeta.GenerateCountSql(querySql);
            command.CommandText = countSql;
            DaoUtils.AppendParameters(command, _dataMeta, query.Parameters);
            long count = QueryByLong(command);
            PageDTO<V> ret = new(count, query.PageSize, query.CurrentPage);
            string pageSql = _dataMeta.GeneratePageSql(querySql, query);
            Type retType = typeof(V);
            bool ifRetMap = false;
            Dictionary<string, MethodParam> methodMap = [];

            if (retType == typeof(Dictionary<string, object>))
            {
                ifRetMap = true;
            }
            else
            {
                methodMap = AnnotationUtils.ReflectObject(retType);
            }
            command.CommandText = pageSql;
            using DbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                ret.Results.Add(DaoUtils.WrapRow<V>(reader, ifRetMap, methodMap, (col) =>
                {
                    string name = reader.GetName(col);
                    if ((mappingMap == null || !mappingMap.MappingColumns.TryGetValue(name, out string? mappingColumn)) && !query.MappingColumns.TryGetValue(name, out mappingColumn))
                    {
                        mappingColumn = StringUtils.CamelCaseUpperConvert(name);
                    }
                    return mappingColumn;
                }));
            }
            return ret;
        }
        public List<V> QueryByCondition<V>(DbCommand command, FilterCondition condition, PageQuery? query = null)
        {
            GetSqlAndParameterByCondition<V>(condition, out StringBuilder builder, out Dictionary<string, object> queryParameter);
            string querySql = builder.ToString();
            DaoUtils.ParseParameter(_dataMeta, command, queryParameter);
            if (query != null)
            {
                querySql = GetDialect().GeneratePageSql(querySql, query);
                string countSql = GetDialect().GenerateCountSql(querySql);
                command.CommandText = countSql;
                long count = QueryByLong(command);
                query.Total = count;
            }
            command.CommandText = querySql;
            bool ifRetMap = false;
            Dictionary<string, MethodParam>? methodMap = null;
            Type retType = typeof(V);
            var retList = new List<V>();
            if (retType == typeof(Dictionary<string, object>))
            {
                ifRetMap = true;
            }
            else
            {
                methodMap = AnnotationUtils.ReflectObject(retType);
            }

            using DbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                retList.Add(DaoUtils.WrapRow<V>(reader, ifRetMap, methodMap));
            }
            return retList;
        }
        public List<O> QueryByFields<O>(Type entityType, DbCommand command, QueryParameter queryParams)
        {
            StringBuilder builder = new();
            string? selectPart = null;
            StringBuilder newColumnsBuilder = new();
            StringBuilder havingBuiler = new();
            StringBuilder groupByBuilder = new();
            Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(entityType);
            Dictionary<string, string> propFieldMap = [];
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(entityType);
            Dictionary<string, int> duplicatedMap = [];
            StringBuilder whereBuilder = new();
            Dictionary<string, object> preparedMap = [];
            if (!queryParams.SelectColumns.IsNullOrEmpty())
            {
                StringBuilder originBuilder = new(Constants.SQL_SELECT);
                foreach (var columnName in queryParams.SelectColumns.Split(','))
                {
                    fieldMap.TryGetValue(columnName, out FieldContent? oContent);
                    Trace.Assert(oContent != null, "property " + columnName + " not defined in Model " + entityType.Name);
                    originBuilder.Append(oContent.FieldName).Append(Constants.SQL_AS).Append(oContent.PropertyName).Append(',');
                }
                originBuilder.Remove(originBuilder.Length - 1, 1);
                selectPart = originBuilder.ToString();
            }
            if (!queryParams.NewColumns.IsNullOrEmpty())
            {
                foreach (var newEntry in queryParams.NewColumns)
                {
                    string asColumnName = newEntry.Key;
                    string? entryValue = newEntry.Value.ToString();
                    Trace.Assert(!string.IsNullOrWhiteSpace(entryValue), "");
                    List<string>? columns = JsonSerializer.Deserialize<List<string>>(entryValue);
                    Trace.Assert(columns != null);
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
                            fieldMap.TryGetValue(selPart, out FieldContent? fieldContent);
                            arithBuilder.Append(fieldContent != null ? fieldContent.FieldName : selPart);
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
                    fieldMap.TryGetValue(columnName, out FieldContent? oContent);
                    Trace.Assert(oContent != null, "property " + columnName + " not defined in Model " + entityType.Name);
                    groupByBuilder.Append(oContent.FieldName).Append(',');
                }
            }
            if (!queryParams.Having.IsNullOrEmpty())
            {
                foreach (var havingEntry in queryParams.Having)
                {
                    string columnName = havingEntry.Key;
                    propFieldMap.TryGetValue(columnName, out string? funcStr);
                    Trace.Assert(!funcStr.IsNullOrEmpty(), "having column alias not defined!");
                    if (havingEntry.Value is JsonElement)
                    {
                        string? havingEntryValue = havingEntry.Value.ToString();
                        Trace.Assert(!string.IsNullOrWhiteSpace(havingEntryValue));
                        Dictionary<string, object>? dict1 = JsonSerializer.Deserialize<Dictionary<string, object>>(havingEntryValue);
                        Trace.Assert(dict1 != null);
                        if (dict1.TryGetValue("operator", out object? operObj))
                        {
                            string? operStr = operObj.ToString();
                            Trace.Assert(!string.IsNullOrWhiteSpace(operStr));
                            Constants.SqlOperator compareOper = Constants.Parse(operStr);
                            double cmpValue = Convert.ToDouble(dict1["values"].ToString());

                            havingBuiler.Append(funcStr).Append(Constants.OperatorValue(compareOper)).Append('@').Append(columnName).Append(Constants.SQL_AND);
                            preparedMap.TryAdd(columnName, cmpValue);
                        }
                    }
                    else
                    {
                        havingBuiler.Append(funcStr).Append('=').Append('@').Append(columnName).Append(Constants.SQL_AND);
                        preparedMap.TryAdd(columnName, Convert.ToDouble(havingEntry.Value.ToString()));
                    }
                }
            }
            string orderByStr = queryParams.OrderBy;

            foreach (var entry in queryParams.Parameters)
            {
                DaoUtils.AppendCondition(entry.Key, entry.Value, fieldMap, whereBuilder, duplicatedMap, preparedMap);
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
            LogUtils.Debug($"query Sql={builder}");

            command.CommandText = builder.ToString();
            bool ifRetMap = false;
            var retList = new List<O>();
            Type retType = typeof(O);
            Dictionary<string, MethodParam>? methodMap = null;
            if (retType.Equals(typeof(Dictionary<string, object>)))
            {
                ifRetMap = true;
            }
            else
            {
                methodMap = AnnotationUtils.ReflectObject(retType);
            }
            DaoUtils.ParseParameter(_dataMeta, command, preparedMap);
            using DbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                retList.Add(DaoUtils.WrapRow<O>(reader, ifRetMap, methodMap));
            }
            return retList;
        }

        public List<dynamic> QueryMapper(SqlSelectSegment sqlsegment, Dictionary<string, object> paramMap, string nameSpace, DbCommand command)
        {
            string rsMap = sqlsegment.ResultMap;
            ResultMap? map = SqlMapperConfigure.GetResultMap(nameSpace, rsMap);
            Type? retType = map?.ModelType;
            bool retMap = false;
            Dictionary<string, MethodParam>? methodMap = null;
            if (retType == null && string.Equals(rsMap, "Map", StringComparison.OrdinalIgnoreCase))
            {
                retMap = true;
                retType = typeof(Dictionary<string, object>);
            }
            else
            {
                Trace.Assert(retType != null, "");
                methodMap = AnnotationUtils.ReflectObject(retType);
            }
            DaoUtils.AppendParameters(command, _dataMeta, paramMap);

            var listType = typeof(List<>).MakeGenericType(retType);

            dynamic? retList = Activator.CreateInstance(listType);

            using DbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Dictionary<string, object>? dict = null;
                object? ret = null;
                if (retMap)
                {
                    dict = [];
                }
                else
                {
                    ret = Activator.CreateInstance(retType);
                }
                Trace.Assert(ret != null);
                for (int col = 0; col < reader.FieldCount; col++)
                {
                    string columnName = reader.GetName(col);
                    if (retMap)
                    {
                        Trace.Assert(dict != null, "");
                        dict[reader.GetName(col)] = reader[col];
                    }
                    else
                    {
                        Trace.Assert(methodMap != null);
                        List<string> columns = methodMap.Keys.ToList();
                        if (map == null || !map.MappingColumns.TryGetValue(columnName, out string? mappingColumn))
                        {
                            mappingColumn = StringUtils.CamelCaseUpperConvert(columnName);
                        }
                        if (DaoUtils.HasColumn(columns, mappingColumn, out string? propName) && !string.IsNullOrWhiteSpace(propName) && methodMap.TryGetValue(propName, out MethodParam? param))
                        {
                            if (!Convert.IsDBNull(reader[col]))
                            {
                                param.Setter(ret, ConvertUtil.ParseByType(param.ParamType, reader[col]));
                            }
                        }
                    }
                }
                if (retMap)
                {
                    retList?.Add(dict);
                }
                else
                {
                    dynamic? tmp = Convert.ChangeType(ret, retType);
                    retList?.Add(tmp);
                }
            }
            IEnumerable? enumerable = retList as IEnumerable;
            Trace.Assert(enumerable != null);
            return enumerable.Cast<dynamic>().ToList();
        }
        public void DoWithQuery(string sql, object[] obj, Action<IDataReader> action)
        {
            using DbConnection connection = _dataMeta.GetDbConnection(_connectionStr);
            connection.Open();
            using DbCommand command = _dataMeta.GetDbCommand(connection, sql);
            DbParameter[] parameters = DaoUtils.ParseParameter(_dataMeta, obj);
            if (parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }
            using DbDataReader reader = command.ExecuteReader();
            action.Invoke(reader);
        }
        public void DoWithQueryNamed(string sql, Dictionary<string, object> queryParameters, Action<IDataReader> action)
        {
            using DbConnection connection = _dataMeta.GetDbConnection(_connectionStr);
            connection.Open();
            using DbCommand command = _dataMeta.GetDbCommand(connection, sql);
            DaoUtils.ParseParameter(_dataMeta, command, queryParameters);
            using (DbDataReader reader = command.ExecuteReader())
            {
                action.Invoke(reader);
            }
        }
        public IQueryable<T> Query<T>() where T : BaseEntity
        {
            return new CustomQueryable<T>(this);
        }
        #endregion
        #region Crud
        public bool SaveEntity(DbCommand command, BaseEntity model, InsertSegment segment)
        {
            command.Parameters.AddRange(segment.Parameters.ToArray());
            command.CommandText = segment.InsertSql;
            if (segment.Increment || segment.Sequence)
            {
                object? genId = command.ExecuteScalar();
                if (genId != null)
                {
                    segment.GenKeySetter(model, ConvertUtil.ParseByType(segment.GenKeyType, genId));
                }
            }
            else
            {
                command.ExecuteNonQuery();
            }
            return true;
        }
        public int SaveEntities<V>(DbCommand command, List<V> models) where V : BaseEntity
        {
            int count = 0;
            foreach (V model in models)
            {
                if (SaveEntity(command, model, SqlUtils.GetInsertSegment(this, model)))
                    count++;
            }
            return count;
        }
        public bool UpdateEntity(DbCommand command, UpdateSegment segment)
        {
            LogUtils.Debug($"execute Update Sql {segment.UpdateSql}");
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

        public void GetSqlAndParameterByCondition<V>(FilterCondition condition, out StringBuilder builder, out Dictionary<string, object> queryParameter)
        {
            queryParameter = [];
            builder = new();
            builder.Append(!condition.SelectParts.IsNullOrEmpty()
                ? condition.SelectParts
                : SqlUtils.GetSelectSql(typeof(V)));

            builder.Append("where ");
            builder.Append(condition.GeneratePreparedSql(queryParameter, [], []));
            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                Log.Debug("using Query {Query}", builder.ToString());
            }
        }


        public int ExecuteMapper(CompositeSegment segment, string nameSpace, string executeId, DbCommand command, Dictionary<string, object> paramMap)
        {

            return command.ExecuteNonQuery();

        }
        public long InsertBatch(DbConnection connection, string schema, string tableName, List<DataSetColumnMeta> metas, IEnumerable<Dictionary<string, object>> models, CancellationToken token, int batchSize = 10000)
        {
            return GetDialect().BatchInsert(this, connection, schema, tableName, metas, models, token);
        }
        public int BatchInsert<T>(DbConnection connection, List<T> list, int batchSize = 1000)
        {
            return GetDialect().BatchInsert(this, connection, list, batchSize);
        }


        #endregion
        public string GetConnectString()
        {
            return _connectionStr;
        }

        public string GetDbTypeStr()
        {
            return _dbTypeStr;
        }

        public Constants.DbType GetDbType()
        {
            return _dbType;
        }
        public ISqlDialect GetDialect()
        {
            return _dataMeta;
        }
        public string GetCurrentSchema()
        {
            return _schema;
        }

        public DbParameter WrapParameter(string key, object value)
        {
            return _dataMeta.WrapParameter(key, value);
        }

        public void Dispose()
        {
            Dispose(true);
        }
        protected void Dispose(bool disposable)
        {

        }
    }
}