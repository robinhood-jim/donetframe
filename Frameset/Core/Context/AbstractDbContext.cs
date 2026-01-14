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
using Frameset.Core.Repo;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Spring.Util;
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
        internal readonly Dictionary<Type, Tuple<EntityContent, IList<FieldContent>>> entityMap = [];
        internal readonly Dictionary<Type, Dictionary<string, FieldContent>> fieldContentMap = [];
        internal readonly Dictionary<Type, List<Type>> subTypeMap = [];
        internal readonly Dictionary<Type, FieldContent> entityPkMap = [];
        internal string dsName;
        internal ThreadLocal<string> temporaryDsName;
        internal ThreadLocal<bool> autoCommitStatus;

        internal ConcurrentDictionary<Thread, UpdateEntry> requestChanges = [];
        internal IJdbcDao dao;
        protected MethodInfo queryByFieldsMethod = typeof(AbstractDbContext).GetMethod("QueryModelsByFieldSimple", BindingFlags.NonPublic | BindingFlags.Instance);
        protected MethodInfo GetExpressionMethod = typeof(ExpressionUtils).GetMethod("GetExpressionFunction", BindingFlags.Static|BindingFlags.Public);
        public string ContextName
        {
            get; set;
        } = DbContextFactory.CONTEXTDEFAULTNAME;


        public bool ExecuteOperation(Action<IJdbcDao, DbCommand> action)
        {
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                DbTransaction transaction = connection.BeginTransaction();
                try
                {
                    DbCommand command = GetDao().GetDialect().GetDbCommand(connection);
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
        public object QueryMapper(string nameSpace, string queryId, object queryObject)
        {
            AbstractSegment segment = SqlMapperConfigure.GetExecuteSegment(nameSpace, queryId);
            if (segment != null)
            {
                AssertUtils.IsTrue(segment.GetType().Equals(typeof(SqlSelectSegment)), "");
                SqlSelectSegment sqlsegment = (SqlSelectSegment)segment;
                Dictionary<string, object> paramMap = new Dictionary<string, object>();

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
        public int ExecuteMapper(string nameSpace, string exeId, object input)
        {
            AbstractSegment segment = SqlMapperConfigure.GetExecuteSegment(nameSpace, exeId);
            if (segment != null)
            {
                AssertUtils.IsTrue(!segment.GetType().Equals(typeof(SqlSelectSegment)), "must not select part");
                AssertUtils.IsTrue(segment.GetType().IsSubclassOf(typeof(CompositeSegment)), "must be composite part");
                Dictionary<string, object> paramMap = [];
                bool returnInsert = false;
                string generateKey = null;
                bool retMap = false;
                Type retType = null;
                Dictionary<string, MethodParam> methodMap = null;
                RepositoryHelper.ExecuteMapperBefore(GetDao(), segment, nameSpace, input, out StringBuilder builder, out paramMap, out retMap, out returnInsert, out generateKey, out methodMap);
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
        protected BaseEntity GetByIdSimple(Type entityType, object pk)
        {
            CheckTypeExists(entityType);
            if (!entityPkMap.TryGetValue(entityType, out FieldContent pkColumn))
            {
                throw new BaseSqlException("pk column not found in Model " + entityType.Name);
            }
            entityMap.TryGetValue(entityType, out Tuple<EntityContent, IList<FieldContent>> tuple);
            string selectSql = SqlUtils.GetSelectByIdSql(entityType, pkColumn);
            IList<Dictionary<string, object>> list = QueryBySql(selectSql, new object[] { pk });
            Func<dynamic> func = (Func<dynamic>)GetExpressionMethod.MakeGenericMethod(entityType).Invoke(null,null);
            object entity = func(); 
            ConstructEntity(list, tuple, entity);
            return entity as BaseEntity;
        }
       

        protected V GetByIdSimple<V, P>(P pk) where V : BaseEntity
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            if (!entityPkMap.TryGetValue(entityType, out FieldContent pkColumn))
            {
                throw new BaseSqlException("pk column not found in Model " + entityType.Name);
            }
            entityMap.TryGetValue(entityType, out Tuple<EntityContent, IList<FieldContent>> tuple);
            string selectSql = SqlUtils.GetSelectByIdSql(entityType, pkColumn);
            IList<Dictionary<string, object>> list = QueryBySql(selectSql, new object[] { pk });
            Func<V> func = ExpressionUtils.GetExpressionFunction<V>();
            V entity = func();
            ConstructEntity(list, tuple, entity);
            return entity;
        }
        private void ConstructEntity(IList<Dictionary<string, object>> list, Tuple<EntityContent, IList<FieldContent>> tuple, object entity)
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
        protected IList<V> QueryModelsByFieldSimple<V>(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null)
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entityType);
            if (!fields.IsNullOrEmpty())
            {
                Tuple<StringBuilder, IList<DbParameter>> tuple = RepositoryHelper.QueryModelByFieldBefore(entityType, GetDao(), fields, propertyName, oper, values, orderByStr);
                using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
                {
                    connection.Open();
                    using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, tuple.Item1.ToString()))
                    {
                        return GetDao().QueryModelsBySql<V>(ExpressionUtils.GetExpressionFunction<V>(), command, tuple.Item2);
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

        public string GetDsName()
        {
            return dsName;
        }
        protected IJdbcDao GetDao()
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

        public abstract V GetById<V, P>(P pk) where V : BaseEntity;

        public abstract long InsertBatch<V>(IEnumerable<V> models, CancellationToken token) where V : BaseEntity;

        public abstract List<O> QueryByCondtion<V, O>(FilterCondition condition) where V : BaseEntity;

        public abstract List<O> QueryByFields<V, O>(QueryParameter queryParams) where V : BaseEntity;

        public abstract List<O> QueryByNamedParameter<O>(string sql, Dictionary<string, object> nameParamter);

        public abstract IList<V> QueryModelsByField<V>(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null) where V : BaseEntity;

        public abstract PageDTO<V> QueryModelsPage<V>(PageQuery query) where V : BaseEntity;

        public abstract PageDTO<O> QueryPage<V, O>(PageQuery query) where V : BaseEntity;

        public abstract int RemoveByFields<V, P>(string fieldName, Constants.SqlOperator sqlOperator, object[] values) where V : BaseEntity;

        public abstract int RemoveEntity<V, P>(IList<P> pks) where V : BaseEntity;

        public abstract int RemoveLogic<V, P>(IList<P> pks, string logicColumn, int status) where V : BaseEntity;
        public abstract void SaveChanges();

        public abstract bool SaveEntity<V>(V entity) where V : BaseEntity;
        public abstract bool UpdateEntity<V, P>(V entity) where V : BaseEntity;
        public abstract void ManyToOne(Type subType, string fieldName, Type parentType);
        public abstract void OneToMany(Type parentType, Type subType, string fieldName, string relationColumn, CascadeType cascadeType);
        protected List<Type> ScanPackage()
        {
            List<Type> retList = [];
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    retList.AddRange(assembly.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract && ((t.GetCustomAttributes(typeof(MappingEntityAttribute), false).Length > 0 && ((MappingEntityAttribute)t.GetCustomAttribute(typeof(MappingEntityAttribute))).DsName.Equals(ContextName)) || t.GetCustomAttributes(typeof(TableAttribute), false).Length > 0))
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
                    IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(type);
                    Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(type);

                    FieldContent pkColumn = fields.First(x => x.IfPrimary);
                    IEnumerator<FieldContent> enumerator = fields.Where(x => x.IsManyToOne).GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        FieldContent oneToManyColumn = enumerator.Current;
                        if (subTypeMap.TryGetValue(oneToManyColumn.ParentEntity, out List<Type> subTypes))
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
    }
}
