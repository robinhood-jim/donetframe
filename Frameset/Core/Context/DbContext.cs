using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Handler;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Core.Repo;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Frameset.Core.Context
{
    public class DbContext : AbstractDbContext
    {
        private readonly Timer timer;
        private readonly AtomicBoolean refreshTag;

        public DbContext(string defaultDs = "core", bool autoScan = true)
        {
            this.dsName = defaultDs;
            this.ContextName = defaultDs;
            if (autoScan)
            {
                List<Type> types = ScanPackage();
                RegisterExists(types.ToArray());
            }
            dao = DAOFactory.GetAdoDao(defaultDs);
            timer = new Timer(new TimerCallback(OnTimerEvent));
            timer.Change(60000 * 2, 60000 * 5);
            refreshTag = new AtomicBoolean(false);
        }
        public void RegisterModels(Type[] models)
        {
            RegisterExists(models);
        }

        public override bool SaveEntity<V>(V entity)
        {
            IMetaObjectHandler handler = GetObjectHandler();
            if (handler != null)
            {
                Dictionary<string, FieldContent> fieldDict = EntityReflectUtils.GetFieldsMap(typeof(V));
                handler.InsertFill(new MetaObject(entity, fieldDict));
            }
            if (IsAutoCommit())
            {
                InsertSegment segment = SqlUtils.GetInsertSegment(GetDao(), entity);
                return RepositoryHelper.ExecuteInTransaction<V, bool>(GetDao(), segment.InsertSql, entity, (command, v) => DoInsert(command, segment, v) > 0);
            }
            else
            {
                GetCurrrentUpdateEntry().Insert(entity);
                return true;
            }
        }

        public override bool UpdateEntity<V, P>(V entity)
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            if (!entityPkMap.TryGetValue(typeof(V), out FieldContent? pkColumn))
            {
                throw new BaseSqlException("pk column not found in Model " + typeof(V).Name);
            }
            object? pkObj = pkColumn.PropertyInfo.GetValue(entity);
            Trace.Assert(pkObj != null);
            P pk = (P)pkObj;

            V origin = GetById<V, P>(pk);
            Trace.Assert(origin != null, "id not found in entity");
            IMetaObjectHandler handler = GetObjectHandler();
            if (handler != null)
            {
                Dictionary<string, FieldContent> fieldDict = EntityReflectUtils.GetFieldsMap(entityType);
                handler.InsertFill(new MetaObject(entity, fieldDict));
            }

            if (IsAutoCommit())
            {
                UpdateSegment segment = SqlUtils.GetUpdateSegment(GetDao(), origin, entity);
                if (segment.UpdateRequired)
                {
                    return RepositoryHelper.ExecuteInTransaction<V, bool>(GetDao(), segment.UpdateSql, entity, (command, v) => DoUpdate(command, segment, entity) > 0);
                }
                return false;
            }
            else
            {
                GetCurrrentUpdateEntry().Update(origin, entity);
                return true;
            }
        }
        public override int RemoveEntity<V, P>(List<P> pks)
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            if (!pks.IsNullOrEmpty())
            {
                if (IsAutoCommit())
                {
                    return RepositoryHelper.ExecuteInTransaction<V, int>(GetDao(), "", Activator.CreateInstance<V>(), (command, v) => DoDelete(command, entityType, pks.Cast<object>().ToList()));
                }
                GetCurrrentUpdateEntry().Delete<V, P>(pks);
                return pks.Count;
            }
            return -1;
        }
        public override int RemoveByFields<V, P>(string fieldName, Constants.SqlOperator sqlOperator, object[] values)
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            if (IsAutoCommit())
            {
                Tuple<string, List<DbParameter>> tuple =
                        SqlUtils.GetRemoveCondition(GetDao(), entityType, fieldName, sqlOperator, values);
                V obj = Activator.CreateInstance<V>();
                return RepositoryHelper.ExecuteInTransaction<V, int>(GetDao(), tuple.Item1, obj,
                        (command, v) => GetDao().Execute(command, tuple.Item1, tuple.Item2.ToArray()));
            }
            List<V> list = QueryModelsByField<V>(fieldName, sqlOperator, values);
            FieldContent pkColumn = EntityReflectUtils.GetPrimaryKey(entityType);
            List<object?>? pkList = list.Select(x => pkColumn.GetMethod.Invoke(x, null)).ToList();
            GetCurrrentUpdateEntry().Delete<V, P>([.. pkList.Cast<P>()]);
            return pkList.Count;
        }



        public override int RemoveLogic<V, P>(List<P> pks, string logicColumn, int status)
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            entityMap.TryGetValue(entityType, out Tuple<EntityContent, List<FieldContent>>? tuple);
            if (!entityPkMap.TryGetValue(typeof(V), out FieldContent? pkColumn))
            {
                throw new BaseSqlException("pk column not found in Model " + typeof(V).Name);
            }
            Trace.Assert(tuple != null);
            if (!pks.IsNullOrEmpty())
            {
                StringBuilder builder = new StringBuilder("update ").Append(SqlUtils.GetTableWithSchema(tuple.Item1)).Append(" set ").Append(logicColumn).Append('=').Append(status).Append(" where ").Append(pkColumn.FieldName).Append(" in (");

                StringBuilder idsBuilder = new();
                DbParameter[] parameters = new DbParameter[pks.Count];
                for (int i = 0; i < pks.Count; i++)
                {
                    string paramName = "@" + (i + 1).ToString();
                    idsBuilder.Append(paramName).Append(',');
                    parameters[i] = GetDao().GetDialect().WrapParameter(i + 1, pks[i]);
                }
                string removeSql = builder.Append(idsBuilder.ToString().AsSpan(0, idsBuilder.Length - 1)).ToString();
                if (IsAutoCommit())
                {
                    V obj = Activator.CreateInstance<V>();
                    return RepositoryHelper.ExecuteInTransaction<V, int>(GetDao(), removeSql, obj, (command, v) =>
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

        public override V GetById<V, P>(P pk, bool ignoreLogic = false)
        {
            Type entityType = typeof(V);
            V entity = GetByIdSimple<V, P>(pk, ignoreLogic);
            if (entityMap.TryGetValue(entityType, out Tuple<EntityContent, List<FieldContent>>? tuple))
            {
                CrudUtils.WrapSubEntity<V>(this, typeof(V), [entity], tuple.Item2);
                return entity;
            }
            throw new NotSupportedException();
        }
        public override long InsertBatch<V>(IEnumerable<V> models, CancellationToken token)
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
        public int BatchInsert<T>(List<T> list, int batchSize = 1000)
        {
            Type entityType = typeof(T);
            CheckTypeExists(entityType);
            using DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString());
            try
            {
                return GetDao().GetDialect().BatchInsert<T>(GetDao(), connection, list, batchSize);
            }
            catch (Exception ex)
            {
                throw new BaseSqlException(ex.Message);
            }
        }
        public override List<V> QueryByCondition<V>(FilterCondition condition, PageQuery? query = null)
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, ""))
                {
                    List<V> retList = GetDao().QueryByCondition<V>(command, condition, query);
                    if (!retList.IsNullOrEmpty())
                    {
                        if (entityMap.TryGetValue(entityType, out Tuple<EntityContent, List<FieldContent>>? tuple))
                        {
                            CrudUtils.WrapSubEntity<V>(this, typeof(V), retList, tuple.Item2);
                        }
                    }
                    return retList;
                }
            }
        }

        public override List<O> QueryByCondition<V, O>(FilterCondition condition)
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            using (DbConnection connection = GetDao().GetDialect().GetDbConnection(GetDao().GetConnectString()))
            {
                connection.Open();
                using (DbCommand command = GetDao().GetDialect().GetDbCommand(connection, ""))
                {
                    return GetDao().QueryByCondition<O>(command, condition);
                }
            }
        }

        public override List<O> QueryByFields<V, O>(QueryParameter queryParams)
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


        public override List<O> QueryByNamedParameter<O>(string sql, Dictionary<string, object> nameParamter)
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

        public override List<V> QueryModelsByField<V>(string propertyName, Constants.SqlOperator oper, object[] values, string? orderByStr = null)
        {
            Type entityType = typeof(V);
            List<V> retList = QueryModelsByFieldSimple<V>(propertyName, oper, values, orderByStr);
            if (!retList.IsNullOrEmpty())
            {
                entityMap.TryGetValue(entityType, out Tuple<EntityContent, List<FieldContent>>? tuple);
                Trace.Assert(tuple != null, "");
                CrudUtils.WrapSubEntity<V>(this, typeof(V), retList, tuple.Item2);
            }
            return retList;
        }

        public override PageDTO<V> QueryModelsPage<V>(PageQuery query)
        {
            Type entityType = typeof(V);
            CheckTypeExists(entityType);
            Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(entityType);
            List<DbParameter> dbParameters = [];
            StringBuilder builder = new StringBuilder(SqlUtils.GetSelectSql(entityType));
            if (query != null)
            {
                if (!query.Parameters.IsNullOrEmpty())
                {
                    builder.Append(" WHERE ");
                }
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
                        List<V> list = GetDao().QueryModelsBySql<V>(command, dbParameters);
                        if (!list.IsNullOrEmpty() && entityMap.TryGetValue(entityType, out Tuple<EntityContent, List<FieldContent>>? tuple))
                        {
                            CrudUtils.WrapSubEntity(this, typeof(V), list, tuple.Item2);
                        }
                        PageDTO<V> ret = new PageDTO<V>(totalCount, query.PageSize, list);

                        return ret;
                    }
                }
            }
            return new PageDTO<V>(0, 0);
        }

        public override PageDTO<O> QueryPage<V, O>(PageQuery query)
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

        public override void ManyToOne(Type subType, string fieldName, Type parentType)
        {
            RegisterExists([subType, parentType]);
            if (entityMap.TryGetValue(subType, out Tuple<EntityContent, List<FieldContent>>? tuple) && fieldContentMap.TryGetValue(subType, out Dictionary<string, FieldContent>? dict))
            {
                tuple.Item1.ParentEntitys.Add(parentType);
                if (dict.TryGetValue(fieldName, out FieldContent? fieldContent))
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
        public override void OneToMany(Type parentType, Type subType, string fieldName, string relationColumn, CascadeType cascadeType = CascadeType.DETACH)
        {
            RegisterExists([subType, parentType]);
            if (entityMap.TryGetValue(subType, out Tuple<EntityContent, List<FieldContent>>? tuple) && fieldContentMap.TryGetValue(subType, out Dictionary<string, FieldContent>? dict))
            {
                tuple.Item1.ParentEntitys.Add(parentType);
                if (dict.TryGetValue(fieldName, out FieldContent? fieldContent))
                {
                    fieldContent.IsManyToOne = true;
                    fieldContent.ParentEntity = parentType;
                    fieldContent.Cascade = cascadeType;
                    if (EntityReflectUtils.GetRelationMap().TryGetValue(parentType, out Dictionary<Type, string>? childMap))
                    {
                        childMap.TryAdd(subType, relationColumn);
                    }
                    else
                    {
                        EntityReflectUtils.GetRelationMap().TryAdd(parentType, new() { { subType, relationColumn } });
                    }
                }
                else
                {
                    throw new BaseSqlException("fieldName not defined in SubModel " + subType.Name);
                }
            }
        }

        public override void SaveChanges()
        {
            if (IsAutoCommit())
            {
                throw new NotSupportedException("SaveChanges must call with not AutoCommit");
            }
            CheckThreadTransaction(out IUnitofWork? unitofWork);
            Trace.Assert(unitofWork != null);
            try
            {
                UpdateEntry entry = GetCurrrentUpdateEntry();
                if (entry != null && !entry.EffectEntrys.IsNullOrEmpty())
                {
                    DbConnection connection = unitofWork.GetConnection();
                    CheckOpen(connection);
                    using DbCommand command = GetDao().GetDialect().GetDbCommand(connection);
                    command.Transaction = unitofWork.GetOrBeginTransaction();
                    int takeAffect = 0;
                    foreach (EffectEntry entry1 in entry.EffectEntrys)
                    {
                        takeAffect += TakeAction(command, entry1);
                    }
                    unitofWork.Commit();

                    if (Log.IsEnabled(LogEventLevel.Debug))
                    {
                        Log.Debug("Save changes Effect row {EffectRow}", takeAffect);
                    }
                    entry.EffectEntrys.Clear();
                }
                else
                {
                    unitofWork.Commit();
                    //Log.Error("no operation contained! Save Change does no effect!");
                }
            }
            catch (Exception ex)
            {
                unitofWork.Rollback();
                throw new BaseSqlException(ex.Message);
            }
            finally
            {
                requestChanges.TryRemove(Thread.CurrentThread, out _);
                unitofWork.Dispose();
                unitOfWorkDict.TryRemove(Thread.CurrentThread, out _);
            }
        }

        private UpdateEntry GetCurrrentUpdateEntry()
        {
            WaitForRefreshTimer().GetAwaiter().GetResult();
            if (requestChanges.TryGetValue(Thread.CurrentThread, out UpdateEntry? entry))
            {
                return entry;
            }
            else
            {
                entry = new UpdateEntry();
                requestChanges.TryAdd(Thread.CurrentThread, entry);
            }
            return entry;
        }
        private async Task WaitForRefreshTimer()
        {
            while (refreshTag.Get())
            {
                Log.Information("refesh thread running,Waiting");
                await Task.Delay(100);
            }
        }
        private int TakeAction(DbCommand command, EffectEntry entry)
        {
            int effectRow = 0;
            IAdoDao dao = GetDao();
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
            effectRow = IncrementExecute(effectRow, GetDao().SaveEntity(command, superEntity, segment));
            //One to Many Insert
            effectRow += DoSaveRelation(command, superEntity);
            return effectRow;
        }
        private int DoUpdate(DbCommand command, UpdateSegment updateSegment, BaseEntity superEntity)
        {
            int effectRow = 0;
            effectRow = IncrementExecute(effectRow, GetDao().UpdateEntity(command, updateSegment));
            //OneToMany Update
            //delete recursive Old and Insert New
            effectRow += DoSaveRelation(command, superEntity);
            return effectRow;
        }
        private int DoSaveRelation(DbCommand command, BaseEntity superEntity)
        {
            int effectRow = 0;
            Type entityType = superEntity.GetType();
            List<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entityType);
            IEnumerable<FieldContent> oneToManys = fields.Where(x => x.IsOneToMany && (x.Cascade == CascadeType.PERSIST || x.Cascade == CascadeType.MERGE || x.Cascade == CascadeType.ALL));
            if (!oneToManys.IsNullOrEmpty())
            {
                FieldContent pkColumn = EntityReflectUtils.GetPrimaryKey(superEntity.GetType());
                object? id = pkColumn.GetMethod.Invoke(superEntity, null);
                Trace.Assert(id != null);
                foreach (FieldContent content in oneToManys.ToList())
                {
                    List<dynamic>? list = content.GetMethod.Invoke(superEntity, null) as List<dynamic>;
                    string subColumn = content.RealtionColumn;
                    if (list != null && !list.IsNullOrEmpty())
                    {
                        Type subType = list.GetType().GetGenericArguments()[0];
                        //remove origin
                        Tuple<string, List<DbParameter>> tuple = SqlUtils.GetRemoveCondition(dao, subType, content.RealtionColumn, Constants.SqlOperator.EQ, [id]);
                        GetDao().Execute(command, tuple.Item1, tuple.Item2.ToArray());

                        Dictionary<string, FieldContent> subFieldMap = EntityReflectUtils.GetFieldsMap(subType);
                        if (!subFieldMap.TryGetValue(subColumn, out FieldContent? realtionContent))
                        {
                            throw new BaseSqlException("refrence column " + subColumn + " not defined in entity " + subType.Name);
                        }
                        FieldContent subPkColumn = EntityReflectUtils.GetPrimaryKey(subType);
                        foreach (object voObj in list)
                        {
                            var sid = subPkColumn.GetMethod.Invoke(voObj, null);
                            BaseEntity? subEntity = voObj as BaseEntity;
                            Trace.Assert(subEntity != null);
                            //数据存在
                            if (sid != null)
                            {
                                if (content.Cascade.Equals(CascadeType.PERSIST) || content.Cascade.Equals(CascadeType.ALL))
                                {
                                    throw new BaseSqlException("id " + id + " already in subType " + subType.Name + ",PRESIST OR ALL INSERT FAILED!");
                                }
                                else
                                {
                                    realtionContent.SetMethod.Invoke(subEntity, [id]);
                                    BaseEntity origin = GetByIdSimple(subType, sid);
                                    UpdateSegment updateSegment1 = SqlUtils.GetUpdateSegment(GetDao(), origin, subEntity);
                                    if (updateSegment1.UpdateRequired)
                                    {
                                        effectRow = IncrementExecute(effectRow, GetDao().UpdateEntity(command, updateSegment1));
                                    }
                                }
                            }
                            else
                            {
                                //找到子表关联字段
                                realtionContent.SetMethod.Invoke(subEntity, [id]);
                                InsertSegment segment1 = SqlUtils.GetInsertSegment(GetDao(), subEntity);
                                effectRow = IncrementExecute(effectRow, GetDao().SaveEntity(command, subEntity, segment1));
                            }

                        }
                    }
                }
            }
            return effectRow;
        }
        private int DoDelete(DbCommand command, Type superEntityType, List<object> pkList)
        {
            int effectRow = 0;
            StringBuilder removeBuilder = new(SqlUtils.GetRemovePkSql(superEntityType));
            StringBuilder idsBuilder = new();
            DbParameter[] parameters = new DbParameter[pkList.Count];
            if (!pkList.IsNullOrEmpty())
            {
                for (int i = 0; i < pkList.Count; i++)
                {
                    string paramName = "@" + (i + 1).ToString();
                    idsBuilder.Append(paramName).Append(',');
                    parameters[i] = GetDao().GetDialect().WrapParameter(i + 1, pkList[i]);
                }
            }
            string removeSql = removeBuilder.Append(idsBuilder.ToString()[..(idsBuilder.Length - 1)]).Append(')').ToString();

            effectRow += GetDao().Execute(command, removeSql, parameters);

            List<FieldContent> fields = EntityReflectUtils.GetFieldsContent(superEntityType);
            IEnumerable<FieldContent> oneToManys = fields.Where(x => x.IsOneToMany && (x.Cascade == CascadeType.REMOVE || x.Cascade == CascadeType.ALL));
            if (!oneToManys.IsNullOrEmpty())
            {
                foreach (FieldContent content in oneToManys.ToList())
                {
                    string subColumn = content.RealtionColumn;
                    Type subType = content.ParamType.GetGenericArguments()[0];
                    Dictionary<string, FieldContent> subFieldMap = EntityReflectUtils.GetFieldsMap(subType);
                    if (!subFieldMap.TryGetValue(subColumn, out FieldContent? relationContent))
                    {
                        throw new BaseSqlException("refrence column " + subColumn + " not defined in entity " + subType.Name);
                    }
                    Tuple<string, List<DbParameter>> tuple1 = SqlUtils.GetRemoveCondition(GetDao(), subType, relationContent.FieldName, Constants.SqlOperator.IN, [pkList]);
                    effectRow += GetDao().Execute(command, tuple1.Item1, tuple1.Item2.ToArray());
                }
            }
            return effectRow;
        }

        private void OnTimerEvent(object? source)
        {
            Trace.Assert(source != null);
            if (!refreshTag.Get())
            {
                refreshTag.Set(true);
                try
                {
                    List<Thread> deleteList = [];
                    foreach (var entry in requestChanges.Select(x => x.Key))
                    {
                        if (!entry.IsAlive)
                        {
                            deleteList.Add(entry);
                        }
                    }
                    if (!deleteList.IsNullOrEmpty())
                    {
                        deleteList.ForEach(x =>
                        {
                            while (!requestChanges.TryRemove(x, out _) && unitOfWorkDict.TryRemove(x, out _))
                            {
                                Thread.Sleep(2);
                            }
                        });
                    }
                }
                finally
                {
                    refreshTag.Set(false);
                }
            }
        }
        protected override void Dispose(bool isDipsose)
        {
            if (isDipsose)
            {
                entityMap.Clear();
                entityPkMap.Clear();
                requestChanges.Clear();
                timer?.Dispose();
                fieldContentMap.Clear();
                temporaryDsName?.Dispose();
                autoCommitStatus?.Dispose();
            }
        }
    }
}
