using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Handler;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Mapper;
using Frameset.Core.Mapper.Segment;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Core.Reflect;
using Frameset.Core.Repo;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Frameset.Core.Context
{
    public abstract class AbstractDbContext : IDbContext
    {
        internal readonly Dictionary<Type, Tuple<EntityContent, List<FieldContent>>> entityMap = [];
        internal readonly Dictionary<Type, Dictionary<string, FieldContent>> fieldContentMap = [];
        internal readonly Dictionary<Type, List<Type>> subTypeMap = [];
        internal readonly Dictionary<Type, FieldContent> entityPkMap = [];
        internal string dsName = null!;
        internal ThreadLocal<string> temporaryDsName = null!;
        internal ThreadLocal<bool> autoCommitStatus = null!;

        internal ConcurrentDictionary<Thread, UpdateEntry> requestChanges = [];
        internal ConcurrentDictionary<Thread, IUnitofWork> unitOfWorkDict = [];
        protected IAdoDao dao = null!;
        protected IMetaObjectHandler metaObjectHandler = null!;
        public string ContextName
        {
            get; set;
        } = DbContextFactory.CONTEXTDEFAULTNAME;


        public bool ExecuteOperation(Action<IAdoDao, DbCommand> action)
        {
            IUnitofWork? unitofWork = null;
            bool autoCommit = IsAutoCommit();
            if (!autoCommit)
            {
                CheckThreadTransaction(out unitofWork);
                Trace.Assert(unitofWork != null);
            }
            GetConnectionAndTransaction(unitofWork, autoCommit, out DbConnection? connection, out DbTransaction? transaction);
            Trace.Assert(connection != null);
            Trace.Assert(transaction != null);
            try
            {
                using DbCommand command = GetDao().GetDialect().GetDbCommand(connection);
                command.Transaction = transaction;
                action.Invoke(GetDao(), command);
                if (autoCommit)
                {
                    transaction.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                Rollback(transaction);
                throw new BaseSqlException(ex.Message);
            }
            finally
            {
                if (autoCommit)
                {
                    DoClear(connection,transaction);
                }
            }
        }

        private void GetConnectionAndTransaction(IUnitofWork? unitofWork, bool autoCommit, out DbConnection? connection, out DbTransaction? transaction)
        {
            connection = autoCommit ? GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()) : unitofWork?.GetConnection();
            Trace.Assert(connection != null);
            CheckOpen(connection);
            transaction = autoCommit ? connection.BeginTransaction() : unitofWork?.GetOrBeginTransaction();
            Trace.Assert(transaction != null);
        }

        public List<dynamic> QueryMapper(string nameSpace, string queryId, object queryObject, List<string>? inputParameters = null)
        {
            AbstractSegment segment = SqlMapperConfigure.GetExecuteSegment(nameSpace, queryId);
            if (segment != null)
            {
                Trace.Assert(segment.GetType().Equals(typeof(SqlSelectSegment)), "");
                SqlSelectSegment sqlsegment = (SqlSelectSegment)segment;
                Dictionary<string, object> paramMap = new Dictionary<string, object>();

                if (queryObject.GetType().IsTypeDictionary())
                {
                    paramMap = (Dictionary<string, object>)queryObject;
                }
                else
                {
                    ConvertUtil.ToDict(queryObject, paramMap);
                }
                if (inputParameters != null && !inputParameters.IsNullOrEmpty())
                {
                    foreach (string parameter in inputParameters)
                    {
                        paramMap.TryAdd(parameter, null);
                    }
                }
                string executeSql = segment.ReturnSqlPart(paramMap);
                LogUtils.Debug($"QueryMapper: {executeSql}");
                using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, executeSql))
                    {
                        return GetDao().QueryMapper(sqlsegment, paramMap, nameSpace, command);
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
                Trace.Assert(!segment.GetType().Equals(typeof(SqlSelectSegment)), "must not select part");
                Trace.Assert(segment.GetType().IsSubclassOf(typeof(CompositeSegment)), "must be composite part");
                IUnitofWork? unitofWork = null;
                bool autoCommit = IsAutoCommit();
                if (!autoCommit)
                {
                    CheckThreadTransaction(out unitofWork);
                    Trace.Assert(unitofWork != null);
                }
                GetConnectionAndTransaction(unitofWork, autoCommit, out DbConnection? connection, out DbTransaction? transaction);
                Trace.Assert(connection != null);
                Trace.Assert(transaction != null);

                RepositoryHelper.ExecuteMapperBefore(GetDao(), segment, nameSpace, input, out StringBuilder builder, out Dictionary<string, object> paramMap, out bool retMap, out bool returnInsert, out string generateKey, out Dictionary<string, MethodParam>? methodMap);
                if (!retMap)
                {
                    ResultMap? map = SqlMapperConfigure.GetResultMap(nameSpace, ((CompositeSegment)segment).ResultMap);
                    Trace.Assert(map != null && map.ModelType != null);
                    methodMap = AnnotationUtils.ReflectObject(map.ModelType);
                }
                int ret = 0;
                try
                {
                    using DbCommand command = GetDao().GetDialect().GetDbCommand(connection, builder.ToString());
                    foreach (var item in paramMap)
                    {
                        command.Parameters.Add(GetDao().GetDialect().WrapParameter("@" + item.Key, item.Value));
                    }
                    if (returnInsert)
                    {
                        object? genId = command.ExecuteScalar();
                        if (genId != null && methodMap != null)
                        {
                            if (!retMap)
                            {
                                if (methodMap.TryGetValue(generateKey, out MethodParam? param))
                                {
                                    param.SetMethod.Invoke(input, new object[] { ConvertUtil.ParseByType(methodMap[generateKey].ParamType, genId) });
                                }
                            }
                            else
                            {
                                paramMap.TryAdd(generateKey, ConvertUtil.ParseByType(methodMap[generateKey].ParamType, genId));
                            }
                        }
                        ret = 1;
                    }
                    else
                    {
                        ret = command.ExecuteNonQuery();
                    }
                    if (autoCommit)
                    {
                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    Rollback(transaction);
                    throw new BaseSqlException(ex.Message);
                }
                finally
                {
                    if (autoCommit)
                    {
                        DoClear(connection, transaction);
                    }
                }
                return ret;
            }
            else
            {
                throw new ConfigMissingException("id " + exeId + " does not found in namespace " + nameSpace);
            }
        }
        internal BaseEntity GetByIdSimple(Type entityType, object pk, bool ignoreLogic = false)
        {
            CheckTypeExists(entityType);
            if (!entityPkMap.TryGetValue(entityType, out FieldContent? pkColumn))
            {
                throw new BaseSqlException("pk column not found in Model " + entityType.Name);
            }
            entityMap.TryGetValue(entityType, out Tuple<EntityContent, List<FieldContent>>? tuple);
            Tuple<string, List<object>> selectSqlTuple = SqlUtils.GetSelectByIdSql(GetDao(), entityType, pkColumn, pk, ignoreLogic);
            List<Dictionary<string, object>> list = QueryBySql(selectSqlTuple.Item1, selectSqlTuple.Item2.ToArray());
            object? entity = Activator.CreateInstance(entityType);
            Trace.Assert(tuple != null && entity != null);
            ConstructEntity(list, tuple, entity);
            BaseEntity? baseEntity = entity as BaseEntity;
            Trace.Assert(baseEntity != null, "");
            return baseEntity;
        }


        protected V GetByIdSimple<V, P>(P pk, bool ignoreLogic = false) where V : BaseEntity where P : notnull
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            if (!entityPkMap.TryGetValue(entityType, out FieldContent? pkColumn))
            {
                throw new BaseSqlException("pk column not found in Model " + entityType.Name);
            }
            entityMap.TryGetValue(entityType, out Tuple<EntityContent, List<FieldContent>>? tuple);
            Tuple<string, List<object>> selectSqlTuple = SqlUtils.GetSelectByIdSql(GetDao(), entityType, pkColumn, pk, ignoreLogic);
            List<Dictionary<string, object>> list = QueryBySql(selectSqlTuple.Item1, selectSqlTuple.Item2.ToArray());
            V entity = Activator.CreateInstance<V>();
            Trace.Assert(tuple != null && entity != null);
            ConstructEntity(list, tuple, entity);
            return entity;
        }
        private void ConstructEntity(List<Dictionary<string, object>> list, Tuple<EntityContent, List<FieldContent>> tuple, object entity)
        {
            if (!list.IsNullOrEmpty())
            {
                if (list.Count > 1)
                {
                    throw new BaseSqlException("id not unique");
                }
                Dictionary<string, object> map = list[0];
                foreach (FieldContent fieldContent in tuple.Item2)
                {
                    if (!fieldContent.IsOneToMany && !fieldContent.IsManyToOne && !fieldContent.IsOneToMany)
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
            }
        }
        internal List<V> QueryModelsByFieldSimple<V>(string propertyName, Constants.SqlOperator oper, object[] values, string? orderByStr = null)
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            List<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entityType);
            if (!fields.IsNullOrEmpty())
            {
                Tuple<StringBuilder, List<DbParameter>> tuple = RepositoryHelper.QueryModelByFieldBefore(entityType, GetDao(), fields, propertyName, oper, values, orderByStr);
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
        internal List<BaseEntity> QueryModelsByFieldSimple(Type entityType, string propertyName, Constants.SqlOperator oper, object[] values, string? orderByStr = null)
        {
            CheckTypeExists(entityType);
            List<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entityType);
            if (!fields.IsNullOrEmpty())
            {
                Tuple<StringBuilder, List<DbParameter>> tuple = RepositoryHelper.QueryModelByFieldBefore(entityType, GetDao(), fields, propertyName, oper, values, orderByStr);
                using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, tuple.Item1.ToString()))
                    {
                        return GetDao().QueryModelsBySql(entityType, command, tuple.Item2);
                    }
                }
            }
            return [];
        }

        internal void CheckTypeExists(Type entityType)
        {
            if (!entityMap.TryGetValue(entityType, out _))
            {
                throw new BaseSqlException("entityType " + entityType.Name + " not register in Context!");
            }
            Trace.Assert(entityType.IsSubclassOf(typeof(BaseEntity)), "Type must sub class of BaseEntity");
        }
        public void RestoreDs()
        {
            temporaryDsName.Dispose();
        }
        public void ChangeDs(string _dsName)
        {
            IAdoDao selectDao = DAOFactory.GetAdoDao(_dsName);
            if (selectDao == null)
            {
                throw new BaseSqlException("dsName " + _dsName + " not registered!");
            }
            temporaryDsName = new ThreadLocal<string>(() => _dsName);
        }

        public string GetDsName()
        {
            if (temporaryDsName != null && temporaryDsName.IsValueCreated)
            {
                string? retDsName = temporaryDsName.Value;
                Trace.Assert(!string.IsNullOrWhiteSpace(retDsName));
                return retDsName;
            }
            else
            {
                return dsName;
            }
        }
        public IAdoDao GetDao()
        {
            if (temporaryDsName != null && temporaryDsName.IsValueCreated)
            {
                string? retDsName = temporaryDsName.Value;
                Trace.Assert(!string.IsNullOrWhiteSpace(retDsName));
                IAdoDao tempDao = DAOFactory.GetAdoDao(retDsName);
                if (tempDao == null)
                {
                    throw new BaseSqlException("dsName " + dsName + " not registered!");
                }
                return tempDao;
            }
            return dao;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposable)
        {
            if (!disposable)
            {
                return;
            }
        }
        public string GetContextName()
        {
            return ContextName;
        }
        public void ResetCommitStatus()
        {
            autoCommitStatus.Dispose();
        }
        public void SetAutoCommit(bool autoCommit)
        {
            autoCommitStatus = new ThreadLocal<bool>(() => autoCommit);
            if (autoCommit)
            {
                if (unitOfWorkDict.TryRemove(Thread.CurrentThread, out IUnitofWork? unitofWork))
                {
                    unitofWork.Dispose();
                }
            }
        }
        protected bool IsAutoCommit()
        {
            if (autoCommitStatus != null && autoCommitStatus.IsValueCreated)
            {
                return autoCommitStatus.Value;
            }
            return true;
        }

        protected int IncrementExecute(int effectRow, bool executeOK)
        {
            if (executeOK)
            {
                return effectRow + 1;
            }
            return effectRow;
        }
        public void DoWithQuery(string sql, Dictionary<string, object> QueryParamter, Action<IDataReader> action)
        {
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, sql))
                {
                    GetDao().DoWithQueryNamed(sql, QueryParamter, action);
                }
            }
        }
        public List<Dictionary<string, object>> QueryBySql(string sql, object[] values)
        {
            IAdoDao queryDao = GetDao();
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, sql))
                {
                    return queryDao.QueryBySql(command, values);
                }
            }
        }
        public int Execute(string sql, DbParameter[] parameters)
        {
            IUnitofWork? unitofWork = null;
            bool autoCommit = IsAutoCommit();
            if (!autoCommit)
            {
                CheckThreadTransaction(out unitofWork);
                Trace.Assert(unitofWork != null);
            }
            GetConnectionAndTransaction(unitofWork, autoCommit, out DbConnection? connection, out DbTransaction? transaction);
            Trace.Assert(connection != null);
            Trace.Assert(transaction != null);
            CheckOpen(connection);
            int returnVal = -1;
            try
            {
                using DbCommand command = GetDao().GetDialect().GetDbCommand(connection, sql);
                returnVal = GetDao().Execute(command, sql, parameters);
                if (autoCommit)
                {
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                Rollback(transaction);
                throw new NotSupportedException(ex.Message);
            }
            finally
            {
                if (autoCommit)
                {
                    DoClear(connection,transaction);
                }
            }
            return returnVal;

        }
        protected IMetaObjectHandler GetObjectHandler()
        {
            return metaObjectHandler ?? RegServiceContext.GetBean<IMetaObjectHandler>();
        }

        public abstract V GetById<V, P>(P pk, bool ignoreLogic = false) where V : BaseEntity where P : notnull;

        public abstract long InsertBatch<V>(IEnumerable<V> models, CancellationToken token) where V : BaseEntity;

        public abstract List<V> QueryByCondition<V>(FilterCondition condition, PageQuery? query = null) where V : BaseEntity;

        public abstract List<O> QueryByCondition<V, O>(FilterCondition condition) where V : BaseEntity;

        public abstract List<O> QueryByFields<V, O>(QueryParameter queryParams) where V : BaseEntity;

        public abstract List<O> QueryByNamedParameter<O>(string sql, Dictionary<string, object> nameParamter);

        public abstract List<V> QueryModelsByField<V>(string propertyName, Constants.SqlOperator oper, object[] values, string? orderByStr = null) where V : BaseEntity;

        public abstract PageDTO<V> QueryModelsPage<V>(PageQuery query) where V : BaseEntity;

        public abstract PageDTO<O> QueryPage<V, O>(PageQuery query) where V : BaseEntity;

        public abstract int RemoveByFields<V, P>(string fieldName, Constants.SqlOperator sqlOperator, object[] values) where V : BaseEntity;

        public abstract int RemoveEntity<V, P>(List<P> pks) where V : BaseEntity where P : notnull;

        public abstract int RemoveLogic<V, P>(List<P> pks, string logicColumn, int status) where V : BaseEntity where P : notnull;
        public abstract void SaveChanges();

        public abstract bool SaveEntity<V>(V entity) where V : BaseEntity;
        public abstract bool UpdateEntity<V, P>(V entity) where V : BaseEntity where P : notnull;
        public abstract void ManyToOne(Type subType, string fieldName, Type parentType);
        public abstract void OneToMany(Type parentType, Type subType, string fieldName, string relationColumn, CascadeType cascadeType = CascadeType.DETACH);
        protected List<Type> ScanPackage()
        {
            List<Type> retList = [];
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    retList.AddRange(assembly.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract && ((t.GetCustomAttributes(typeof(MappingEntityAttribute), false).Length > 0 && (t.GetCustomAttribute(typeof(MappingEntityAttribute)) as MappingEntityAttribute).DsName.Equals(ContextName)) || t.GetCustomAttributes(typeof(TableAttribute), false).Length > 0))
                                .ToList());
                }
                catch (Exception ex)
                {
                    Log.Error("{Message}", ex.Message);
                }
            }
            return retList;
        }
        protected void RegisterExists(Type[] types)
        {
            if (!types.IsNullOrEmpty())
            {
                foreach (Type type in types)
                {
                    if (entityMap.TryGetValue(type, out _))
                    {
                        continue;
                    }
                    Trace.Assert(type.IsSubclassOf(typeof(BaseEntity)), "Type must sub class of BaseEntity");
                    EntityContent content = EntityReflectUtils.GetEntityInfo(type);
                    List<FieldContent> fields = EntityReflectUtils.GetFieldsContent(type);
                    Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(type);
                    IEnumerable<FieldContent>? pkList = fields.Where(x => x.IfPrimary);
                    FieldContent? pkColumn = !pkList.IsNullOrEmpty() ? pkList.First() : null;
                    if (pkColumn == null)
                    {
                        LogUtils.Error($"Entity {type.Name} does not have primary key,Skipped");
                        continue;
                    }
                    IEnumerator<FieldContent> enumerator = fields.Where(x => x.IsManyToOne).GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        FieldContent oneToManyColumn = enumerator.Current;
                        if (subTypeMap.TryGetValue(oneToManyColumn.ParentEntity, out List<Type>? subTypes))
                        {
                            subTypes.Add(type);
                        }
                        else
                        {
                            subTypeMap.TryAdd(oneToManyColumn.ParentEntity, [type]);
                        }
                    }
                    entityPkMap.TryAdd(type, pkColumn);
                    entityMap.TryAdd(type, Tuple.Create(content, fields));
                    fieldContentMap.TryAdd(type, fieldMap);
                }
            }
        }
        protected void CheckOpen(DbConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
        }
        protected void CheckThreadTransaction(out IUnitofWork? unitofWork)
        {
            if (!unitOfWorkDict.TryGetValue(Thread.CurrentThread, out unitofWork))
            {
                unitofWork = new UnitofWork(GetDao());
                unitOfWorkDict.TryAdd(Thread.CurrentThread, unitofWork);
            }
            unitofWork?.GetOrBeginTransaction();
        }
        protected void Rollback(DbTransaction transaction)
        {
            if (!IsAutoCommit())
            {
                CheckThreadTransaction(out IUnitofWork? unitofWork);
                unitofWork?.Rollback();
                unitOfWorkDict.TryRemove(Thread.CurrentThread, out _);
            }
            else
            {
                transaction.Rollback();
            }
        }
        protected void DoClear(DbConnection? connection,DbTransaction? transaction)
        {
            transaction?.Dispose();
            if (connection != null) {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
                connection.Dispose();
            }
            transaction = null;
            connection = null;
        }
    }
}
