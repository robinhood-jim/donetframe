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
using Serilog.Events;
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
using System.Threading.Tasks;


namespace Frameset.Core.Context
{
    public class DbContext : IDbContext
    {
        private readonly ConcurrentDictionary<Type, Tuple<EntityContent, IList<FieldContent>>> entityMap = [];
        private readonly ConcurrentDictionary<Type, Dictionary<string, FieldContent>> fieldContentMap = [];
        private readonly ConcurrentDictionary<Type, List<Type>> subTypeMap = [];
        private readonly ConcurrentDictionary<Type, FieldContent> entityPkMap = [];
        private readonly string dsName;
        protected ThreadLocal<string> temporaryDsName;
        protected ThreadLocal<bool> autoCommitStatus;

        protected ConcurrentDictionary<int, UpdateEntry> requestChanges = [];
        protected ConcurrentDictionary<int, long> lastOperationTimeMap = [];
        protected IJdbcDao dao;
        private readonly Timer timer;
        public bool DeleteRecusive
        {
            get; set;
        } = false;
        public string ContextName
        {
            get; set;
        } = DbContextFactory.CONTEXTDEFAULTNAME;
        private readonly AtomicBoolean refreshTag;
        //dbTransaction max changes Contain Time 2Hours
        private readonly long MAXTRANSACTIONSECONDS = 60 * 60 * 2;
        public DbContext(string defaultDs = "core", bool autoScan = true)
        {
            this.dsName = defaultDs;
            if (autoScan)
            {
                List<Type> types = ScanPackage();
                RegisterExists(types.ToArray());
            }
            dao = DAOFactory.GetJdbcDao(defaultDs);
            //timer = new Timer(new TimerCallback(OnTimerEvent));
            //timer.Change(60000*10, 60000 * 30);
            refreshTag = new AtomicBoolean(false);
        }
        public void RegisterModels(Type[] models)
        {
            RegisterExists(models);
        }

        public bool SaveEntity<V>(V entity) where V : BaseEntity
        {

            if (IsAutoCommit())
            {
                InsertSegment segment = SqlUtils.GetInsertSegment(GetDao(), entity);
                return RepositoryHelper.ExecuteInTransaction<V, bool>(GetDao(), segment.InsertSql, entity, (command, v) =>
                {
                    int effectRow = DoInsert(command, segment, v);
                    return effectRow > 0;
                });
            }
            else
            {
                GetCurrrentUpdateEntry().Insert(entity);
                return true;
            }
        }

        public bool UpdateEntity<V, P>(V entity) where V : BaseEntity
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            if (!entityPkMap.TryGetValue(typeof(V), out FieldContent pkColumn))
            {
                throw new BaseSqlException("pk column not found in Model " + typeof(V).Name);
            }

            V origin = GetById<V, P>((P)pkColumn.GetMethod.Invoke(entity, null));
            Trace.Assert(origin != null, "id not found in entity");

            if (IsAutoCommit())
            {
                UpdateSegment segment = SqlUtils.GetUpdateSegment(GetDao(), origin, entity);
                return RepositoryHelper.ExecuteInTransaction<V, bool>(GetDao(), segment.UpdateSql, entity, (command, v) =>
                {
                    int effectRow = DoUpdate(command, segment, entity);
                    return effectRow > 0;
                });
            }
            else
            {
                GetCurrrentUpdateEntry().Update(origin, entity);
                return true;
            }
        }
        public int RemoveEntity<V, P>(IList<P> pks) where V : BaseEntity
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            if (!pks.IsNullOrEmpty())
            {
                if (IsAutoCommit())
                {

                    return RepositoryHelper.ExecuteInTransaction<V, int>(GetDao(), "", null, (command, v) =>
                    {
                        return DoDelete(command, entityType, pks.Cast<object>().ToList());
                    });
                }
                else
                {
                    GetCurrrentUpdateEntry().Delete<V, P>(pks);
                    return pks.Count;
                }

            }
            return -1;
        }
        public int RemoveByFields<V, P>(string fieldName, Constants.SqlOperator sqlOperator, object[] values) where V : BaseEntity
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            if (IsAutoCommit())
            {
                Tuple<StringBuilder, IList<DbParameter>> tuple = SqlUtils.GetRemoveCondition(GetDao(), entityType, fieldName, sqlOperator, values);
                return RepositoryHelper.ExecuteInTransaction<V, int>(GetDao(), tuple.Item1.ToString(), null, (command, v) =>
                {
                    return GetDao().Execute(command, tuple.Item1.ToString(), tuple.Item2.ToArray());
                });
            }
            else
            {
                IList<V> list = QueryModelsByField<V>(fieldName, sqlOperator, values);
                FieldContent pkColumn = EntityReflectUtils.GetPriamryKey(entityType);
                List<object> pkList = list.Select(x => pkColumn.GetMethod.Invoke(x, null)).ToList();
                GetCurrrentUpdateEntry().Delete<V, P>(pkList.Cast<P>().ToList());
                return pkList.Count;
            }
        }

        public int RemoveLogic<V, P>(IList<P> pks, string logicColumn, int status) where V : BaseEntity
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            entityMap.TryGetValue(entityType, out Tuple<EntityContent, IList<FieldContent>> tuple);
            if (!entityPkMap.TryGetValue(typeof(V), out FieldContent pkColumn))
            {
                throw new BaseSqlException("pk column not found in Model " + typeof(V).Name);
            }
            if (!pks.IsNullOrEmpty())
            {
                StringBuilder builder = new StringBuilder("update ").Append(SqlUtils.GetTableWithSchema(tuple.Item1)).Append(" set ").Append(logicColumn).Append("=").Append(status).Append(" where ").Append(pkColumn.FieldName).Append(" in (");

                StringBuilder idsBuilder = new StringBuilder();
                DbParameter[] parameters = new DbParameter[pks.Count];
                for (int i = 0; i < pks.Count; i++)
                {
                    string paramName = "@" + (i + 1).ToString();
                    idsBuilder.Append(paramName).Append(",");
                    parameters[i] = GetDao().GetDialect().WrapParameter(i + 1, pks[i]);
                }
                string removeSql = builder.Append(idsBuilder.ToString().Substring(0, idsBuilder.Length - 1)).ToString();
                if (IsAutoCommit())
                {
                    return RepositoryHelper.ExecuteInTransaction<V, int>(GetDao(), removeSql, null, (command, v) =>
                    {
                        return GetDao().Execute(command, removeSql, parameters);
                    });
                }
                else
                {
                    GetCurrrentUpdateEntry().DeleteLogic<V, P>(pks, logicColumn, status);
                    return pks.Count;
                }
            }
            return -1;
        }

        public V GetById<V, P>(P pk)
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            if (!entityPkMap.TryGetValue(typeof(V), out FieldContent pkColumn))
            {
                throw new BaseSqlException("pk column not found in Model " + typeof(V).Name);
            }
            entityMap.TryGetValue(entityType, out Tuple<EntityContent, IList<FieldContent>> tuple);
            string selectSql = SqlUtils.GetSelectByIdSql(typeof(V), pkColumn);
            IList<Dictionary<string, object>> list = QueryBySql(selectSql, new object[] { pk });
            V entity = Activator.CreateInstance<V>();

            if (!list.IsNullOrEmpty())
            {
                if (list.Count > 1)
                {
                    throw new BaseSqlException("id not unique");
                }

                Dictionary<string, object> map = list[0];
                foreach (FieldContent fieldContent in tuple.Item2)
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


        public long InsertBatch<V>(IEnumerable<V> models, CancellationToken token) where V : BaseEntity
        {
            Type entityType = typeof(V);
            
            CheckTypeExists(entityType);

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

        public List<O> QueryByCondtion<V, O>(FilterCondition condition) where V : BaseEntity
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, ""))
                {
                    return GetDao().QueryByConditon<O>(command, condition);
                }
            }
        }

        public List<O> QueryByFields<V, O>(QueryParameter queryParams) where V : BaseEntity
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, ""))
                {
                    return GetDao().QueryByFields<O>(entityType, command, queryParams);
                }
            }
        }

        public List<O> QueryByNamedParameter<O>(string sql, Dictionary<string, object> nameParamter)
        {
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, sql))
                {
                    return GetDao().QueryByNamedParameter<O>(command, nameParamter);
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

        public IList<V> QueryModelsByField<V>(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null) where V : BaseEntity
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
                        return GetDao().QueryModelsBySql<V>(entityType, command, tuple.Item2);
                    }
                }
            }
            return [];

        }

        public PageDTO<V> QueryModelsPage<V>(PageQuery query) where V : BaseEntity
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
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
                        IList<V> list = GetDao().QueryModelsBySql<V>(entityType, command);

                        PageDTO<V> ret = new PageDTO<V>(totalCount, query.PageSize);
                        ret.Results = list;
                        return ret;
                    }
                }
            }
            return null;
        }

        public PageDTO<O> QueryPage<V, O>(PageQuery query) where V : BaseEntity
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection))
                {
                    return GetDao().QueryPage<O>(command, query);
                }
            }
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
        public void ManyToOne(Type subType,string fieldName,Type parentType)
        {
            RegisterExists([subType, parentType]);
            if(entityMap.TryGetValue(subType,out Tuple<EntityContent,IList<FieldContent>> tuple) && fieldContentMap.TryGetValue(subType,out Dictionary<string,FieldContent> dict))
            {
                tuple.Item1.ParentEntitys.Add(parentType);
                if(dict.TryGetValue(fieldName,out FieldContent fieldContent))
                {
                    fieldContent.IsManyToOne = true;
                    fieldContent.ParentEntity = parentType;
                }
                else
                {
                    throw new BaseSqlException("fieldName not defined in SubModel " + subType.Name);
                }
            }
        }
        public void OneToMany(Type parentType,Type subType,string fieldName)
        {
            RegisterExists([subType, parentType]);
            if (entityMap.TryGetValue(subType, out Tuple<EntityContent, IList<FieldContent>> tuple) && fieldContentMap.TryGetValue(subType, out Dictionary<string, FieldContent> dict))
            {
                tuple.Item1.ParentEntitys.Add(parentType);
                if (dict.TryGetValue(fieldName, out FieldContent fieldContent))
                {
                    fieldContent.IsManyToOne = true;
                    fieldContent.ParentEntity = parentType;
                }
                else
                {
                    throw new BaseSqlException("fieldName not defined in SubModel " + subType.Name);
                }
            }
        }
        public void ManyToMany(Type targetType,List<Tuple<Type,string>> entitys)
        {

        }
        public void SaveChanges()
        {
            int currentOperId = Thread.CurrentThread.ManagedThreadId;
            try
            {
                UpdateEntry entry = GetCurrrentUpdateEntry();
                if (entry != null && !entry.EffectEntrys.IsNullOrEmpty())
                {
                    int effectRow = RepositoryHelper.ExecuteInTransaction(GetDao(), (connection) =>
                    {
                        DbCommand command = dao.GetDialect().GetDbCommand(connection);
                        int takeAffect = 0;
                        foreach (EffectEntry entry in entry.EffectEntrys)
                        {
                            takeAffect += TakeAction(command, entry);
                        }
                        return takeAffect;
                    });
                    if (Log.IsEnabled(LogEventLevel.Debug))
                    {
                        Log.Debug("Save changes Effect row {EffectRow}", effectRow);
                    }
                    entry.EffectEntrys.Clear();
                }
                else
                {
                    Log.Error("no operation contained! Save Change does no effect!");
                }
            }
            finally
            {
                requestChanges.Remove(currentOperId,out _);
                lastOperationTimeMap.Remove(currentOperId,out _);
            }
        }
        
        private UpdateEntry GetCurrrentUpdateEntry()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            WaitForRefreshTimer();
            if (requestChanges.TryGetValue(threadId, out UpdateEntry entry))
            {
                return entry;
            }
            else
            {
                entry = new UpdateEntry();
                requestChanges.TryAdd(threadId, entry);
            }
            lastOperationTimeMap[threadId] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return entry;
        }
        private async void WaitForRefreshTimer()
        {
            while (refreshTag.Get())
            {
                Log.Information("refesh thread running,Waiting");
                await Task.Delay(1000);
            }
        }
        private int TakeAction(DbCommand command, EffectEntry entry)
        {
            int effectRow = 0;
            IJdbcDao dao = GetDao();
            switch (entry.EffectType)
            {
                case EFFECTTYPE.Insert:
                    InsertSegment segment = SqlUtils.GetInsertSegment(dao, entry.Entity);
                    effectRow = DoInsert(command, segment, entry.Entity);
                    break;
                case EFFECTTYPE.Update:
                    UpdateSegment updateSegment = SqlUtils.GetUpdateSegment(GetDao(), entry.OriginEntity, entry.Entity);
                    effectRow = DoUpdate(command, updateSegment, entry.Entity);
                    break;
                case EFFECTTYPE.Delete:
                    effectRow = DoDelete(command, entry.Entity.GetType(), entry.PkList);
                    break;
                case EFFECTTYPE.DeleteLogic:

                    break;

            }
            return effectRow;
        }
        private int DoInsert(DbCommand command, InsertSegment segment, BaseEntity superEntity)
        {
            int effectRow = 0;
            if (Log.IsEnabled(LogEventLevel.Debug))
            {
                Log.Debug("SaveChanges with Sql {InsertSql}", segment.InsertSql);
            }
            bool executeRs = dao.SaveEntity(command, superEntity, segment);
            effectRow = increment(effectRow, executeRs);
            //One to Many Insert
            if (!superEntity.GetSubEntities().IsNullOrEmpty())
            {
                FieldContent pkColumn = EntityReflectUtils.GetPriamryKey(superEntity.GetType());
                object id = pkColumn.GetMethod.Invoke(superEntity, null);
                Type subType = superEntity.GetSubEntities()[0].GetType();
                EntityContent subEntityContent = EntityReflectUtils.GetEntityInfo(subType);
                FieldContent subContent = EntityReflectUtils.GetFieldsContent(subType).First(x => x.IsManyToOne);
                Trace.Assert(subEntityContent.ParentEntitys.Contains(superEntity.GetType()),"subType "+subType.Name+" is Not defined as ManyToOne Attribute");
                foreach (BaseEntity subEntity in superEntity.GetSubEntities())
                {
                    subContent.SetMethod.Invoke(superEntity, [id]);
                    InsertSegment segment1 = SqlUtils.GetInsertSegment(dao, subEntity);
                    if (Log.IsEnabled(LogEventLevel.Debug))
                    {
                        Log.Debug("SaveChanges with Sql {InsertSql}", segment1.InsertSql);
                    }
                    executeRs = dao.SaveEntity(command, subEntity, segment1);
                    effectRow = increment(effectRow, executeRs);
                }
            }
            return effectRow;
        }
        private int DoUpdate(DbCommand command, UpdateSegment updateSegment, BaseEntity superEntity)
        {
            int effectRow = 0;
            bool executeRs = GetDao().UpdateEntity(command, updateSegment);
            effectRow = increment(effectRow, executeRs);
            //delete recursive Old and Insert New
            if (!superEntity.GetSubEntities().IsNullOrEmpty())
            {
                FieldContent pkColumn = EntityReflectUtils.GetPriamryKey(superEntity.GetType());
                object id = pkColumn.GetMethod.Invoke(superEntity, null);
                Type subType = superEntity.GetSubEntities()[0].GetType();
                EntityContent subEntityContent = EntityReflectUtils.GetEntityInfo(subType);
                FieldContent subContent = EntityReflectUtils.GetFieldsContent(subType).First(x => x.IsManyToOne);
                Trace.Assert(subEntityContent.ParentEntitys.Contains(superEntity.GetType()), "subType " + subType.Name + " is Not defined as ManyToOne Attribute");
                //Remove By Field
                Tuple<StringBuilder, IList<DbParameter>> tuple = SqlUtils.GetRemoveCondition(GetDao(), subType, subContent.FieldName, Constants.SqlOperator.EQ, [id]);
                effectRow += dao.Execute(command, tuple.Item1.ToString(), tuple.Item2.ToArray());
                foreach (BaseEntity entity in superEntity.GetSubEntities())
                {
                    subContent.SetMethod.Invoke(entity, [id]);
                    InsertSegment segment1 = SqlUtils.GetInsertSegment(dao, entity);
                    if (Log.IsEnabled(LogEventLevel.Debug))
                    {
                        Log.Debug("SaveChanges with Sql {InsertSql}", segment1.InsertSql);
                    }
                    executeRs = dao.SaveEntity(command, entity, segment1);
                    effectRow = increment(effectRow, executeRs);
                }
            }
            return effectRow;
        }
        private int DoDelete(DbCommand command, Type superEntityType, List<object> pkList)
        {
            int effectRow = 0;
            StringBuilder removeBuilder = new StringBuilder(SqlUtils.GetRemovePkSql(superEntityType));
            StringBuilder idsBuilder = new StringBuilder();
            DbParameter[] parameters = new DbParameter[pkList.Count];
            if (!pkList.IsNullOrEmpty())
            {
                for (int i = 0; i < pkList.Count; i++)
                {
                    string paramName = "@" + (i + 1).ToString();
                    idsBuilder.Append(paramName).Append(",");
                    parameters[i] = dao.GetDialect().WrapParameter(i + 1, pkList[i]);
                }
            }
            string removeSql = removeBuilder.Append(idsBuilder.ToString().Substring(0, idsBuilder.Length - 1)).Append(")").ToString();

            effectRow += dao.Execute(command, removeSql, parameters);
            //delete Recusive
            if (subTypeMap.TryGetValue(superEntityType, out List<Type> subTypes) && !subTypes.IsNullOrEmpty() && DeleteRecusive)
            {
                foreach (Type subType in subTypes)
                {
                    FieldContent subContent = EntityReflectUtils.GetFieldsContent(subType).First(x => x.IsManyToOne);
                    Tuple<StringBuilder, IList<DbParameter>> tuple1 = SqlUtils.GetRemoveCondition(GetDao(), subType, subContent.FieldName, Constants.SqlOperator.IN, [pkList]);
                    effectRow += dao.Execute(command, tuple1.Item1.ToString(), tuple1.Item2.ToArray());
                }
            }
            return effectRow;
        }
        private int increment(int effectRow, bool executeOK)
        {
            if (executeOK)
            {
                return effectRow + 1;
            }
            return effectRow;
        }
        private void OnTimerEvent(object source)
        {
            if (!refreshTag.Get())
            {
                refreshTag.Set(true);
                try
                {
                    long currentTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    List<int> deleteList = [];
                    foreach(var entry in lastOperationTimeMap)
                    {
                        if (currentTs - entry.Value > MAXTRANSACTIONSECONDS)
                        {
                            Log.Error("thread {ThreadId} Transaction Wait exceed Max TimeOut,May be MemoryLeak or bugs,Clear all changes!", entry.Key);
                            deleteList.Add(entry.Key);
                        }
                    }
                    if (!deleteList.IsNullOrEmpty())
                    {
                        deleteList.ForEach(x =>
                        {
                            lastOperationTimeMap.Remove(x,out _);
                            requestChanges.Remove(x, out _);
                        });
                    }
                }
                finally
                {
                    refreshTag.Set(false);
                }
            }
        }

        private List<Type> ScanPackage()
        {
            List<Type> retList = [];
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    retList.AddRange(assembly.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract && ((t.GetCustomAttributes(typeof(MappingEntityAttribute), false).Length > 0 && ((MappingEntityAttribute)t.GetCustomAttribute(typeof(MappingEntityAttribute))).DbContextName.Equals(ContextName)) || t.GetCustomAttributes(typeof(TableAttribute), false).Length > 0))
                                .ToList());
                }
                catch (Exception ex)
                {
                    Log.Error("{Message}", ex.Message);
                }
            }
            return retList;
        }
        private void CheckTypeExists(Type entityType)
        {
            if (!entityMap.TryGetValue(entityType, out _))
            {
                throw new BaseSqlException("entityType " + entityType.Name + " not register in Context!");
            }
            Trace.Assert(entityType.IsSubclassOf(typeof(BaseEntity)), "Type must sub class of BaseEntity");
        }


        private void RegisterExists(Type[] types)
        {
            if (!types.IsNullOrEmpty())
            {
                foreach (Type type in types)
                {
                    if(entityMap.TryGetValue(type,out _))
                    {
                        continue;
                    }
                    Trace.Assert(type.IsSubclassOf(typeof(BaseEntity)), "Type must sub class of BaseEntity");
                    EntityContent content = EntityReflectUtils.GetEntityInfo(type);
                    IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(type);
                    Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(type);

                    FieldContent pkColumn = fields.First(x => x.IfPrimary);
                    IEnumerator<FieldContent> enumerator= fields.Where(x => x.IsManyToOne).GetEnumerator();
                    while (enumerator.MoveNext()) { 
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
        private bool IsAutoCommit()
        {
            if (autoCommitStatus != null && autoCommitStatus.IsValueCreated)
            {
                return autoCommitStatus.Value;
            }
            return true;
        }
        public void Dispose()
        {
            entityMap.Clear();
            entityPkMap.Clear();
            requestChanges.Clear();
            timer.Dispose();
            fieldContentMap.Clear();
            temporaryDsName.Dispose();
            autoCommitStatus.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
