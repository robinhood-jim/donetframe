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
using System.Linq;
using System.Reflection;
using System.Text;


namespace Frameset.Core.Repo
{
    public class BaseRepository<V, P> where V : BaseEntity
    {
        //多数据源标识
        internal string dsName = "core";
        internal EntityContent content;
        internal FieldContent pkColumn;

        internal Func<V, bool> saveFunc;
        internal Func<V, bool> updateFunc;
        internal Func<IList<P>, int> deleteFunc;
        internal Action<V> insertBeforeAction;
        internal Action<V> updateBeforeAction;
        internal Action<V> deleteBeforeAction;
        internal Func<DbCommand, V, bool> insertAfterAction;
        internal Func<DbCommand, V, bool> updateAfterAction;
        internal Action<DbCommand, V> deleteAfterAction;
        internal Func<DbConnection, DbTransaction> transcationFunc;
        internal IJdbcDao dao;
        private Type entityType;
        private Type pkType;
        private IList<FieldContent> fields;

        internal BaseRepository()
        {
            Type[] genericType = GetType().GetGenericArguments();
            AssertUtils.IsTrue(genericType[0].IsSubclassOf(typeof(BaseEntity)));
            content = EntityReflectUtils.GetEntityInfo(genericType[0]);
            fields = EntityReflectUtils.GetFieldsContent(genericType[0]);
            pkColumn = fields.Where(x => x.IfPrimary).ToList()[0];
            entityType = genericType[0];
            pkType = genericType[1];
            if (content.DsName != null)
            {
                dsName = content.DsName;
            }
            dao = DAOFactory.getInstance().getJdbcDao(dsName);
        }
        public bool SavenEntity(V entity)
        {
            InsertSegment segment = SqlUtils.GetInsertSegment(dao, entity);
            if (saveFunc != null)
            {
                return saveFunc.Invoke(entity);
            }
            return executeInTransaction<bool>(segment.InsertSql, entity, (command, v) =>
            {
                bool executeRs = false;
                if (insertBeforeAction != null)
                {
                    insertBeforeAction.Invoke(v);
                }
                executeRs = dao.SaveEntity(command, v, segment);

                if (insertAfterAction != null)
                {
                    executeRs = insertAfterAction.Invoke(command, v);
                }
                return executeRs;
            });

        }
        public bool UpdateEntity(V entity)
        {
            UpdateSegment segment = SqlUtils.GetUpdateSegment(dao, entity);
            if (updateFunc != null)
            {
                return updateFunc.Invoke(entity);
            }
            else
            {
                return executeInTransaction<bool>(segment.UpdateSql, entity, (command, v) =>
                {
                    bool executeRs = false;
                    if (updateBeforeAction != null)
                    {
                        updateBeforeAction.Invoke(v);
                    }
                    executeRs = dao.UpdateEntity(command, v, segment);

                    if (insertAfterAction != null)
                    {
                        executeRs = insertAfterAction.Invoke(command, v);
                    }
                    return executeRs;
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
                        parameters[i] = dao.GetDialect().WrapParameter(i + 1, pks[i]);
                    }
                }
                string removeSql = removeBuilder.Append(idsBuilder.ToString().Substring(0, idsBuilder.Length - 1)).ToString();
                return executeInTransaction<int>(removeSql, null, (command, v) =>
                {
                    return dao.Execute(command, removeSql, parameters);
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
                    parameters[i] = dao.GetDialect().WrapParameter(i + 1, pks[i]);
                }
                string removeSql = builder.Append(idsBuilder.ToString().Substring(0, idsBuilder.Length - 1)).ToString();
                return executeInTransaction<int>(removeSql, null, (command, v) =>
                {
                    return dao.Execute(command, removeSql, parameters);
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
                foreach (FieldContent content in fields)
                {
                    object value = map[content.PropertyName];
                    if (Convert.IsDBNull(value) && map.ContainsKey(content.PropertyName.ToLower()))
                    {
                        value = map[content.PropertyName.ToLower()];
                    }
                    if (Convert.IsDBNull(value) && map.ContainsKey(content.PropertyName.ToUpper()))
                    {
                        value = map[content.PropertyName.ToUpper()];
                    }
                    if (!Convert.IsDBNull(value))
                    {
                        content.SetMethold.Invoke(entity, new object[] { ConvertUtil.parseByType(content.GetMethold.ReturnType, value) });
                    }
                }

            }
            return entity;

        }
        public IList<Dictionary<string, object>> QueryBySql(string sql, object[] values)
        {
            IJdbcDao dao = DAOFactory.getInstance().getJdbcDao(dsName);
            using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = dao.GetDialect().GetDbCommand(connection, sql))
                {
                    return dao.QueryBySql(command, values);
                }
            }
        }
        public IList<V> QueryModelsByField(PropertyInfo info, Constants.SqlOperator oper, object[] values, string orderByStr = null)
        {
            string propName = info.Name;
            Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(entityType);
            FieldContent content = null;
            if (fieldMap.TryGetValue(propName, out content))
            {
                StringBuilder builder = new StringBuilder(SqlUtils.GetSelectSql(entityType)).Append(" WHERE ");
                IList<DbParameter> parameters = ParameterHelper.AddQueryParam(dao, content, builder, 0, oper, values);
                if (!orderByStr.IsNullOrEmpty())
                {
                    builder.Append(" order by ").Append(orderByStr);
                }
                using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = dao.GetDialect().GetDbCommand(connection, builder.ToString()))
                    {
                        return dao.QueryModelsBySql<V>(entityType, command, parameters);
                    }
                }
            }
            else
            {

                throw new BaseSqlException("");
            }

        }
        public IList<V> QueryModelsByField(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null)
        {
            IList<V> retList = new List<V>();
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entityType);
            if (!fields.IsNullOrEmpty())
            {
                FieldContent content = fields.Where(x => string.Equals(x.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase)).First();
                if (content == null)
                {
                    content = fields.Where(x => string.Equals(x.FieldName, propertyName, StringComparison.OrdinalIgnoreCase)).First();
                }
                if (content == null)
                {
                    throw new BaseSqlException("propertyName " + propertyName + " not found in Model");
                }
                StringBuilder builder = new StringBuilder(SqlUtils.GetSelectSql(entityType)).Append(" WHERE ");

                IList<DbParameter> parameters = ParameterHelper.AddQueryParam(dao, content, builder, 0, oper, values);
                if (!orderByStr.IsNullOrEmpty())
                {
                    builder.Append(" order by ").Append(orderByStr);
                }
                using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = dao.GetDialect().GetDbCommand(connection, builder.ToString()))
                    {
                        return dao.QueryModelsBySql<V>(entityType, command, parameters);
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
                if (!sqlsegment.Parametertype.IsNullOrEmpty())
                {
                    ConvertUtil.ToDict(queryObject, paramMap);
                }
                else if (queryObject.GetType().Equals(typeof(Dictionary<string, object>)))
                {
                    paramMap = (Dictionary<string, object>)queryObject;
                }
                string executeSql = segment.ReturnSqlPart(paramMap);
                using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = dao.GetDialect().GetDbCommand(connection, executeSql))
                    {
                        return dao.QueryMapper(sqlsegment, paramMap, nameSpace, command, queryObject);
                    }
                }
            }
            else
            {
                throw new ConfigMissingException("id " + queryId + " does not found in namespace " + nameSpace);
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

                StringBuilder builder = new StringBuilder(segment.ReturnSqlPart(paramMap));
                bool returnInsert = false;
                string generateKey = null;
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
                using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = dao.GetDialect().GetDbCommand(connection, builder.ToString()))
                    {
                        if (!paramMap.IsNullOrEmpty())
                        {
                            foreach (var item in paramMap)
                            {
                                command.Parameters.Add(dao.GetDialect().WrapParameter("@" + item.Key, item.Value));
                            }
                            if (returnInsert)
                            {
                                object genId = command.ExecuteScalar();
                                if (genId != null)
                                {
                                    if (!retMap)
                                    {
                                        methodMap[generateKey].SetMethod.Invoke(input, new object[] { ConvertUtil.parseByType(methodMap[generateKey].ParamType, genId) });
                                    }
                                    else
                                    {
                                        paramMap[generateKey] = ConvertUtil.parseByType(methodMap[generateKey].ParamType, genId);
                                    }
                                }
                                return 1;
                            }
                            else
                            {
                                return command.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            throw new ConfigMissingException("all property is null");
                        }
                    }
                }
            }
            else
            {
                throw new ConfigMissingException("id " + exeId + " does not found in namespace " + nameSpace);
            }
        }
        public PageDTO<O> QueryPage<O>(PageQuery query)
        {
            using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = dao.GetDialect().GetDbCommand(connection, ""))
                {
                    return dao.QueryPage<O>(command, query);
                }
            }
        }
        public int InsertBatch(IList<V> models)
        {
            DataTable dataTable = new DataTable();
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entityType);
            InsertSegment segment = SqlUtils.GetInsertSegment(dao, models[0]);

            using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
            {
                connection.Open();

                try
                {
                    return dao.GetDialect().BatchInsert<V>(dao, connection, models);

                }
                catch (Exception ex)
                {
                    throw new BaseSqlException(ex.Message);
                }
            }
        }


        internal T executeInTransaction<T>(string sql, V entity, Func<DbCommand, V, T> func)
        {
            using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
            {
                connection.Open();
                DbTransaction transaction = openTransaction(connection);
                try
                {
                    DbCommand command = dao.GetDialect().GetDbCommand(connection, sql);
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
        internal virtual DbTransaction openTransaction(DbConnection connection)
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
    public class Builder<V, P> where V : BaseEntity
    {
        private BaseRepository<V, P> repository;
        public Builder()
        {
            repository = new BaseRepository<V, P>();
        }
        public Builder<V, P> SaveFunction(Func<V, bool> insertFunction)
        {
            repository.saveFunc = insertFunction;
            return this;
        }
        public Builder<V, P> UpdateFunction(Func<V, bool> updateFunc)
        {
            repository.updateFunc = updateFunc;
            return this;
        }
        public Builder<V, P> SaveBeforeAction(Action<V> action)
        {
            repository.insertBeforeAction = action;
            return this;
        }
        public Builder<V, P> UpdateBeforeAction(Action<V> action)
        {
            repository.updateBeforeAction = action;
            return this;
        }
        public Builder<V, P> DeleteBeforeAction(Action<V> action)
        {
            repository.deleteBeforeAction = action;
            return this;
        }
        public Builder<V, P> DeleteAfterAction(Action<DbCommand, V> action)
        {
            repository.deleteAfterAction = action;
            return this;
        }
        public Builder<V, P> SaveAfterAction(Func<DbCommand, V, bool> action)
        {
            repository.insertAfterAction = action;
            return this;
        }
        public Builder<V, P> UpdateAfterAction(Func<DbCommand, V, bool> action)
        {
            repository.updateAfterAction = action;
            return this;
        }
        public Builder<V, P> TransctionManager(Func<DbConnection, DbTransaction> func)
        {
            repository.transcationFunc = func;
            return this;
        }
        public Builder<V, P> DeleteFunc(Func<IList<P>, int> func)
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
