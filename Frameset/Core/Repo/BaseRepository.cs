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
        //DataSource Tag
        internal string dsName = "core";
        internal EntityContent content;
        internal FieldContent pkColumn;

        internal Func<V, bool> saveFunc = null!;
        internal Func<V, bool> updateFunc = null!;
        internal Func<IList<P>, int> deleteFunc = null!;

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
            pkColumn = fieldContents.First(x => x.IfPrimary);
            entityType = genericType[0];
            pkType = genericType[1];
            if (content.DsName != null && dsName.IsNullOrEmpty())
            {
                dsName = content.DsName;
            }
            dao = DAOFactory.GetJdbcDao(dsName);

        }
        public bool SaveEntity(V entity, Action<V> insertBeforeAction = null, Func<DbCommand, V, bool> insertAfterAction = null)
        {
            InsertSegment segment = SqlUtils.GetInsertSegment(GetDao(), entity);
            if (saveFunc != null)
            {
                return saveFunc.Invoke(entity);
            }
            return RepositoryHelper.ExecuteInTransaction<V, bool>(GetDao(), segment.InsertSql, entity, (command, v) =>
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

        public bool UpdateEntity(V entity, Action<V> updateBeforeAction = null, Func<DbCommand, V, bool> updateAfterAction = null)
        {
            V origin = GetById((P)pkColumn.GetMethod.Invoke(entity, null));
            Trace.Assert(origin != null, "id not found in entity");
            UpdateSegment segment = SqlUtils.GetUpdateSegment(GetDao(), origin, entity);
            if (updateFunc != null)
            {
                return updateFunc.Invoke(entity);
            }
            else
            {
                return RepositoryHelper.ExecuteInTransaction<V, bool>(GetDao(), segment.UpdateSql, entity, (command, v) =>
                {
                    bool? executeRs = false;
                    updateBeforeAction?.Invoke(v);
                    executeRs = GetDao().UpdateEntity(command, segment);
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
                return RepositoryHelper.ExecuteInTransaction<V, int>(GetDao(), removeSql, null, (command, v) =>
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
                return RepositoryHelper.ExecuteInTransaction<V, int>(GetDao(), removeSql, null, (command, v) =>
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
            V entity = Activator.CreateInstance<V>();

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
                        fieldContent.SetMethod.Invoke(entity, new object[] { ConvertUtil.ParseByType(fieldContent.ParamType, value) });
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
        public List<O> QueryByNamedParameter<O>(string sql, Dictionary<string, object> nameParamter)
        {
            IJdbcDao queryDao = GetDao();
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, sql))
                {
                    return queryDao.QueryByNamedParameter<O>(command, nameParamter);
                }
            }
        }

        public IList<V> QueryModelsByField(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null)
        {
            IList<V> retList = new List<V>();
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entityType);
            if (!fields.IsNullOrEmpty())
            {
                Tuple<StringBuilder, IList<DbParameter>> tuple = RepositoryHelper.QueryModelByFieldBefore(entityType, GetDao(), fields, propertyName, oper, values, orderByStr);
                using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, tuple.Item1.ToString()))
                    {
                        return GetDao().QueryModelsBySql<V>(command, tuple.Item2);
                    }
                }
            }
            return [];
        }
        public PageDTO<V> QueryModelsPage(PageQuery query)
        {
            Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(entityType);
            List<DbParameter> dbParameters = [];
            StringBuilder builder = new StringBuilder(SqlUtils.GetSelectSql(entityType)).Append(" WHERE ");
            if (query != null && !query.Parameters.IsNullOrEmpty())
            {

                RepositoryHelper.GetQueryParam(GetDao(), query, fieldMap, dbParameters, builder);
                string querySql = GetDao().GetDialect().GeneratePageSql(builder.ToString(), query);
                string countSql = GetDao().GetDialect().GenerateCountSql(builder.ToString());

                using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, countSql))
                    {

                        long totalCount = GetDao().QueryByLong(command, dbParameters);
                        command.CommandText = querySql;
                        IList<V> list = GetDao().QueryModelsBySql<V>(command, dbParameters);

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
                CompositeSegment csegment = (CompositeSegment)segment;
                Dictionary<string, object> paramMap = [];
                bool returnInsert = false;
                string generateKey = null;
                bool retMap = false;
                Type retType = null;
                Dictionary<string, MethodParam> methodMap = null;
                StringBuilder builder = new StringBuilder();
                RepositoryHelper.ExecuteMapperBefore(GetDao(), segment, nameSpace, input, out builder, out paramMap, out retMap, out returnInsert, out generateKey, out methodMap);
                if (!retMap)
                {
                    methodMap = AnnotationUtils.ReflectObject(retType);
                }
                return RepositoryHelper.ExecuteInTransaction(GetDao(), builder.ToString(), (command) =>
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

}
