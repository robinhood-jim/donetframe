using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Mapper;
using Frameset.Core.Mapper.Segment;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;
using Spring.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;


namespace Frameset.Core.Repo
{
    public class BaseRepository<V, P> : IBaseRepository<V, P> where V : BaseEntity
    {
        //多数据源标识
        internal string dsName = "core";
        internal EntityContent content;
        internal FieldContent pkColumn;

        internal Func<V, bool> saveFunc = null!;
        internal Func<V, bool> updateFunc = null!;
        internal Func<IList<P>, int> deleteFunc = null!;
        internal Action<V> insertBeforeAction = null!;
        internal Action<V> updateBeforeAction = null!;
        internal Action<V> deleteBeforeAction = null!;
        internal Func<DbCommand, V, bool> insertAfterAction = null!;
        internal Func<DbCommand, V, bool> updateAfterAction = null!;
        internal Action<DbCommand, V> deleteAfterAction = null!;
        internal Func<DbConnection, DbTransaction> transcationFunc = null!;
        internal IJdbcDao dao;
        protected readonly Type entityType;
        protected readonly Type pkType;
        protected IList<FieldContent> fieldContents;
        protected ThreadLocal<string> temporaryDsName;
        public BaseRepository()
        {
            Type[] genericType = GetType().GetInterfaces()[0].GetGenericArguments();
            RepositoryAttribute attribute = (RepositoryAttribute)GetType().GetCustomAttribute(typeof(RepositoryAttribute));
            if (attribute != null && !attribute.DsName.IsNullOrEmpty())
            {
                dsName = attribute.DsName;
            }
            AssertUtils.IsTrue(genericType[0].IsSubclassOf(typeof(BaseEntity)));
            content = EntityReflectUtils.GetEntityInfo(genericType[0]);
            fieldContents = EntityReflectUtils.GetFieldsContent(genericType[0]);
            pkColumn = fieldContents.Where(x => x.IfPrimary).ToList()[0];
            entityType = genericType[0];
            pkType = genericType[1];
            if (content.DsName != null && dsName.IsNullOrEmpty())
            {
                dsName = content.DsName;
            }
            dao = DAOFactory.GetJdbcDao(dsName);
        }
        public bool SaveEntity(V entity)
        {
            InsertSegment segment = SqlUtils.GetInsertSegment(GetDao(), entity);
            if (saveFunc != null)
            {
                return saveFunc.Invoke(entity);
            }
            return ExecuteInTransaction<bool>(segment.InsertSql, entity, (command, v) =>
            {
                bool? executeRs = false;

                insertBeforeAction?.Invoke(v);
                executeRs = GetDao().SaveEntity(command, v, segment);
                if (insertAfterAction != null)
                {
                    executeRs = insertAfterAction?.Invoke(command, v);
                }
                return executeRs.HasValue;
            });

        }
        public bool UpdateEntity(V entity)
        {
            UpdateSegment segment = SqlUtils.GetUpdateSegment(GetDao(), entity);
            if (updateFunc != null)
            {
                return updateFunc.Invoke(entity);
            }
            else
            {
                return ExecuteInTransaction<bool>(segment.UpdateSql, entity, (command, v) =>
                {
                    bool? executeRs = false;
                    updateBeforeAction?.Invoke(v);
                    executeRs = GetDao().UpdateEntity(command, v, segment);
                    if (updateAfterAction != null)
                    {
                        executeRs = updateAfterAction?.Invoke(command, v);
                    }
                    return executeRs.HasValue;
                });
            }
        }

        public int RemoveEntity(IList<P> pks)
        {
            if (!pks.IsNullOrEmpty())
            {
                if (deleteFunc != null)
                {
                    return deleteFunc.Invoke(pks);
                }

                StringBuilder idsBuilder = new StringBuilder();
                DbParameter[] parameters = new DbParameter[pks.Count];
                StringBuilder removeBuilder = new StringBuilder(SqlUtils.GetRemovePkSql(entityType));
                if (!pks.IsNullOrEmpty())
                {
                    for (int i = 0; i < pks.Count; i++)
                    {
                        string paramName = "@" + (i + 1).ToString();
                        idsBuilder.Append(paramName).Append(",");
                        parameters[i] = GetDao().GetDialect().WrapParameter(i + 1, pks[i]);
                    }
                }
                string removeSql = removeBuilder.Append(idsBuilder.ToString().Substring(0, idsBuilder.Length - 1)).Append(")").ToString();
                return ExecuteInTransaction<int>(removeSql, null, (command, v) =>
                {
                    return GetDao().Execute(command, removeSql, parameters);
                });

            }
            return -1;
        }
        public int RemoveLogic(IList<P> pks, string logicColumn, int status)
        {
            if (!pks.IsNullOrEmpty())
            {
                StringBuilder builder = new StringBuilder("update ").Append(SqlUtils.GetTableWithSchema(content)).Append(" set ").Append(logicColumn).Append("=").Append(status).Append(" where ").Append(pkColumn.FieldName).Append(" in (");

                StringBuilder idsBuilder = new StringBuilder();
                DbParameter[] parameters = new DbParameter[pks.Count];
                for (int i = 0; i < pks.Count; i++)
                {
                    string paramName = "@" + (i + 1).ToString();
                    idsBuilder.Append(paramName).Append(",");
                    parameters[i] = GetDao().GetDialect().WrapParameter(i + 1, pks[i]);
                }
                string removeSql = builder.Append(idsBuilder.ToString().Substring(0, idsBuilder.Length - 1)).ToString();
                return ExecuteInTransaction<int>(removeSql, null, (command, v) =>
                {
                    return GetDao().Execute(command, removeSql, parameters);
                });
            }
            return -1;
        }
        public V GetById(P pk)
        {
            string selectSql = SqlUtils.GetSelectByIdSql(entityType, pkColumn);
            IList<Dictionary<string, object>> list = QueryBySql(selectSql, new object[] { pk });
            V entity = System.Activator.CreateInstance<V>();

            if (!list.IsNullOrEmpty())
            {
                if (list.Count > 1)
                {
                    throw new BaseSqlException("id not unique");
                }

                Dictionary<string, object> map = list[0];
                foreach (FieldContent fieldContent in fieldContents)
                {
                    object value = map[fieldContent.PropertyName];
                    if (Convert.IsDBNull(value) && map.ContainsKey(fieldContent.PropertyName.ToLower()))
                    {
                        value = map[fieldContent.PropertyName.ToLower()];
                    }
                    if (Convert.IsDBNull(value) && map.ContainsKey(fieldContent.PropertyName.ToUpper()))
                    {
                        value = map[fieldContent.PropertyName.ToUpper()];
                    }
                    if (!Convert.IsDBNull(value))
                    {
                        fieldContent.SetMethod.Invoke(entity, new object[] { ConvertUtil.ParseByType(fieldContent.GetMethod.ReturnType, value) });
                    }
                }

            }
            return entity;

        }
        public IList<Dictionary<string, object>> QueryBySql(string sql, object[] values)
        {
            IJdbcDao queryDao = GetDao();
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, sql))
                {
                    return queryDao.QueryBySql(command, values);
                }
            }
        }

        public IList<V> QueryModelsByField(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null)
        {
            IList<V> retList = new List<V>();
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entityType);
            if (!fields.IsNullOrEmpty())
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

                IList<DbParameter> parameters = ParameterHelper.AddQueryParam(GetDao(), fielContent, builder, 0, out int endPos, oper, values);
                if (!orderByStr.IsNullOrEmpty())
                {
                    builder.Append(" order by ").Append(orderByStr);
                }
                using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, builder.ToString()))
                    {
                        return GetDao().QueryModelsBySql<V>(entityType, command, parameters);
                    }
                }
            }
            return null;
        }
        public PageDTO<V> QueryModelsPage(PageQuery query)
        {
            Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(entityType);
            List<DbParameter> dbParameters = [];
            StringBuilder builder = new StringBuilder(SqlUtils.GetSelectSql(entityType)).Append(" WHERE ");
            if (query != null && !query.Parameters.IsNullOrEmpty())
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
                        IList<DbParameter> parameters = ParameterHelper.AddQueryParam(GetDao(), fieldContent, builder, currentParamCount, out int addCount, oper, values.ToArray());
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
                string querySql = GetDao().GetDialect().GeneratePageSql(builder.ToString(), query);
                string countSql = GetDao().GetDialect().GenerateCountSql(builder.ToString());

                using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, countSql))
                    {

                        long totalCount = GetDao().QueryByLong(command, dbParameters);
                        command.CommandText = querySql;
                        IList<V> list = GetDao().QueryModelsBySql<V>(entityType, command);

                        PageDTO<V> ret = new PageDTO<V>(totalCount, query.PageSize);
                        ret.Results = list;
                        return ret;
                    }
                }
            }
            return null;
        }
        public object QueryMapper(string nameSpace, string queryId, object queryObject)
        {
            AbstractSegment segment = SqlMapperConfigure.GetExecuteSegment(nameSpace, queryId);
            if (segment != null)
            {
                AssertUtils.IsTrue(segment.GetType().Equals(typeof(SqlSelectSegment)), "");
                SqlSelectSegment sqlsegment = (SqlSelectSegment)segment;
                Dictionary<string, object> paramMap = new Dictionary<string, object>();
                string rsMap = sqlsegment.ResultMap;

                if (queryObject.GetType().Equals(typeof(Dictionary<string, object>)))
                {
                    paramMap = (Dictionary<string, object>)queryObject;
                }
                else
                {
                    ConvertUtil.ToDict(queryObject, paramMap);
                }
                string executeSql = segment.ReturnSqlPart(paramMap);
                using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, executeSql))
                    {
                        return GetDao().QueryMapper(sqlsegment, paramMap, nameSpace, command, queryObject);
                    }
                }
            }
            else
            {
                throw new ConfigMissingException("id " + queryId + " does not found in namespace " + nameSpace);
            }
        }
        public bool ExecuteOperation(Action<IJdbcDao, DbCommand> action)
        {
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                DbTransaction transaction = connection.BeginTransaction();
                try
                {
                    DbCommand command = GetDao().GetDialect().GetDbCommand(connection, "");
                    command.Transaction = transaction;
                    action.Invoke(GetDao(), command);
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new BaseSqlException(ex.Message);
                }
            }
        }

        public int ExecuteMapper(string nameSpace, string exeId, object input)
        {
            AbstractSegment segment = SqlMapperConfigure.GetExecuteSegment(nameSpace, exeId);
            if (segment != null)
            {
                AssertUtils.IsTrue(!segment.GetType().Equals(typeof(SqlSelectSegment)), "must not select part");
                AssertUtils.IsTrue(segment.GetType().IsSubclassOf(typeof(CompositeSegment)), "must be composite part");
                Dictionary<string, object> paramMap = new Dictionary<string, object>();
                CompositeSegment csegment = (CompositeSegment)segment;
                string rsMap = csegment.Parametertype;
                if (!csegment.Parametertype.IsNullOrEmpty())
                {
                    ConvertUtil.ToDict(input, paramMap);
                }
                else if (input.GetType().Equals(typeof(Dictionary<string, object>)))
                {
                    paramMap = (Dictionary<string, object>)input;
                }

                ResultMap map = SqlMapperConfigure.GetResultMap(nameSpace, rsMap);
                Type retType = map != null ? map.ModelType : Type.GetType(rsMap);
                bool retMap = false;
                Dictionary<string, MethodParam> methodMap = null;
                if (retType == null && string.Equals(rsMap, "Map", StringComparison.OrdinalIgnoreCase))
                {
                    retMap = true;
                }
                else
                {
                    methodMap = AnnotationUtils.ReflectObject(retType);
                }

                StringBuilder builder = new(segment.ReturnSqlPart(paramMap));
                bool returnInsert = false;
                string generateKey = null;
                if (csegment.GetType().Equals(typeof(SqlInsertSegment)))
                {
                    SqlInsertSegment insertSegment = (SqlInsertSegment)segment;

                    if (insertSegment.UseGenerateKey)
                    {
                        builder.Append(GetDao().GetDialect().AppendKeyHolder());
                        returnInsert = true;
                    }
                    else if (!insertSegment.SequenceName.IsNullOrEmpty())
                    {
                        builder.Append(GetDao().GetDialect().AppendSequence(insertSegment.SequenceName));
                        returnInsert = true;
                    }
                    generateKey = insertSegment.KeyProperty;

                }
                Trace.Assert(!paramMap.IsNullOrEmpty(), "all property is null");

                return ExecuteInTransaction(builder.ToString(), (command) =>
                {
                    foreach (var item in paramMap)
                    {
                        command.Parameters.Add(GetDao().GetDialect().WrapParameter("@" + item.Key, item.Value));
                    }
                    if (returnInsert)
                    {
                        object genId = command.ExecuteScalar();
                        if (genId != null)
                        {
                            if (!retMap)
                            {
                                methodMap.TryGetValue(generateKey, out MethodParam param);
                                param?.SetMethod.Invoke(input, new object[] { ConvertUtil.ParseByType(methodMap[generateKey].ParamType, genId) });
                            }
                            else
                            {
                                paramMap.TryAdd(generateKey, ConvertUtil.ParseByType(methodMap[generateKey].ParamType, genId));
                            }
                        }
                        return 1;
                    }
                    else
                    {
                        return command.ExecuteNonQuery();
                    }
                });
            }
            else
            {
                throw new ConfigMissingException("id " + exeId + " does not found in namespace " + nameSpace);
            }
        }

        public PageDTO<O> QueryPage<O>(PageQuery query)
        {
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, ""))
                {
                    return GetDao().QueryPage<O>(command, query);
                }
            }
        }
        public long InsertBatch(IEnumerable<V> models, CancellationToken token)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entityType);

            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                try
                {
                    return GetDao().GetDialect().BatchInsert<V>(GetDao(), connection, models, token);
                }
                catch (Exception ex)
                {
                    throw new BaseSqlException(ex.Message);
                }
            }
        }
        public List<O> QueryByCondtion<O>(FilterCondition condition)
        {
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, ""))
                {
                    return GetDao().QueryByConditon<O>(command, condition);
                }
            }
        }
        public List<O> QueryByFields<O>(QueryParameter queryParams)
        {
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, ""))
                {
                    return GetDao().QueryByFields<O>(entityType, command, queryParams);
                }
            }
        }
        public string GetDsName()
        {
            return dsName;
        }
        public void ChangeDs(string dsName)
        {
            IJdbcDao selectDao = DAOFactory.GetJdbcDao(dsName);
            if (selectDao == null)
            {
                throw new BaseSqlException("dsName " + dsName + " not registered!");
            }
            else
            {
                temporaryDsName = new ThreadLocal<string>(() => dsName);
            }
        }
        public void RestoreDs()
        {
            temporaryDsName.Dispose();
        }
        private IJdbcDao GetDao()
        {
            if (temporaryDsName != null && temporaryDsName.IsValueCreated)
            {
                IJdbcDao tempDao = DAOFactory.GetJdbcDao(temporaryDsName.Value);
                if (tempDao == null)
                {
                    throw new BaseSqlException("dsName " + dsName + " not registered!");
                }
                return tempDao;
            }
            return dao;
        }
        internal int ExecuteInTransaction(string sql, Func<DbCommand, int> func)
        {
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                DbTransaction transaction = connection.BeginTransaction();
                try
                {
                    DbCommand command = GetDao().GetDialect().GetDbCommand(connection, sql);
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

        internal T ExecuteInTransaction<T>(string sql, V entity, Func<DbCommand, V, T> func)
        {
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                DbTransaction transaction = connection.BeginTransaction();
                try
                {
                    DbCommand command = GetDao().GetDialect().GetDbCommand(connection, sql);
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
            }
        }
        internal virtual DbTransaction OpenTransaction(DbConnection connection)
        {
            if (transcationFunc == null)
            {
                return connection.BeginTransaction();
            }
            else
            {
                return transcationFunc.Invoke(connection);
            }
        }
    }
    public class RepositoryBuilder<V, P> where V : BaseEntity
    {
        private readonly BaseRepository<V, P> repository;
        public RepositoryBuilder()
        {
            repository = new BaseRepository<V, P>();
        }
        public RepositoryBuilder<V, P> SaveFunction(Func<V, bool> insertFunction)
        {
            repository.saveFunc = insertFunction;
            return this;
        }
        public RepositoryBuilder<V, P> UpdateFunction(Func<V, bool> updateFunc)
        {
            repository.updateFunc = updateFunc;
            return this;
        }
        public RepositoryBuilder<V, P> SaveBeforeAction(Action<V> action)
        {
            repository.insertBeforeAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> UpdateBeforeAction(Action<V> action)
        {
            repository.updateBeforeAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> DeleteBeforeAction(Action<V> action)
        {
            repository.deleteBeforeAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> DeleteAfterAction(Action<DbCommand, V> action)
        {
            repository.deleteAfterAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> SaveAfterAction(Func<DbCommand, V, bool> action)
        {
            repository.insertAfterAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> UpdateAfterAction(Func<DbCommand, V, bool> action)
        {
            repository.updateAfterAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> TransctionManager(Func<DbConnection, DbTransaction> func)
        {
            repository.transcationFunc = func;
            return this;
        }
        public RepositoryBuilder<V, P> DeleteFunc(Func<IList<P>, int> func)
        {
            repository.deleteFunc = func;
            return this;
        }
        public BaseRepository<V, P> Build()
        {
            return repository;
        }
    }
}
