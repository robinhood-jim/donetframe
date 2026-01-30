using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Dao;
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
            dao = DAOFactory.GetJdbcDao(defaultDs);
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

        public override bool UpdateEntity<V, P>(V entity)
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
                if (segment.UpdateRequired)
                {
                    return RepositoryHelper.ExecuteInTransaction<V, bool>(GetDao(), segment.UpdateSql, entity, (command, v) =>
                    {
                        int effectRow = DoUpdate(command, segment, entity);
                        return effectRow > 0;
                    });
                }
                return false;
            }
            else
            {
                GetCurrrentUpdateEntry().Update(origin, entity);
                return true;
            }
        }
        public override int RemoveEntity<V, P>(IList<P> pks)
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
        public override int RemoveByFields<V, P>(string fieldName, Constants.SqlOperator sqlOperator, object[] values)
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
                FieldContent pkColumn = EntityReflectUtils.GetPrimaryKey(entityType);
                List<object> pkList = list.Select(x => pkColumn.GetMethod.Invoke(x, null)).ToList();
                GetCurrrentUpdateEntry().Delete<V, P>([.. pkList.Cast<P>()]);
                return pkList.Count;
            }
        }

        public override int RemoveLogic<V, P>(IList<P> pks, string logicColumn, int status)
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

        public override V GetById<V, P>(P pk)
        {
            Type entityType = typeof(V);
            V entity = GetByIdSimple<V, P>(pk);
            entityMap.TryGetValue(entityType, out Tuple<EntityContent, IList<FieldContent>> tuple);
            WrapSubEntity<V>(typeof(V), [entity], tuple.Item2);
            return entity;
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

        public override List<O> QueryByCondtion<V, O>(FilterCondition condition)
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

        public override IList<V> QueryModelsByField<V>(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null)
        {
            IList<V> retList = QueryModelsByFieldSimple<V>(propertyName, oper, values, orderByStr);
            return retList;
        }

        public override PageDTO<V> QueryModelsPage<V>(PageQuery query)
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
                        IList<V> list = GetDao().QueryModelsBySql<V>(command, dbParameters);

                        PageDTO<V> ret = new PageDTO<V>(totalCount, query.PageSize);
                        ret.Results = list;
                        return ret;
                    }
                }
            }
            return null;
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
        public override void OneToMany(Type parentType, Type subType, string fieldName, string relationColumn, CascadeType cascadeType = CascadeType.DETACH)
        {
            RegisterExists([subType, parentType]);
            if (entityMap.TryGetValue(subType, out Tuple<EntityContent, IList<FieldContent>> tuple) && fieldContentMap.TryGetValue(subType, out Dictionary<string, FieldContent> dict))
            {
                tuple.Item1.ParentEntitys.Add(parentType);
                if (dict.TryGetValue(fieldName, out FieldContent fieldContent))
                {
                    fieldContent.IsManyToOne = true;
                    fieldContent.ParentEntity = parentType;
                    fieldContent.Cascade = cascadeType;
                    if (EntityReflectUtils.GetRelationMap().TryGetValue(parentType, out Dictionary<Type, string> childMap))
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
            try
            {
                UpdateEntry entry = GetCurrrentUpdateEntry();
                if (entry != null && !entry.EffectEntrys.IsNullOrEmpty())
                {
                    int effectRow = RepositoryHelper.ExecuteInTransaction(GetDao(), (connection) =>
                    {
                        DbCommand command = GetDao().GetDialect().GetDbCommand(connection);
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
                requestChanges.Remove(Thread.CurrentThread, out _);
            }
        }
        private void WrapSubEntity<V>(Type entityType, List<V> retList, IList<FieldContent> fields)
        {
            FieldContent pkColumn = EntityReflectUtils.GetPrimaryKey(entityType);
            Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(entityType);
            IEnumerable<FieldContent> manyToOnes = fields.Where(x => x.IsManyToOne);
            if (!manyToOnes.IsNullOrEmpty())
            {
                foreach (V entity in retList)
                {
                    List<FieldContent> contentList = manyToOnes.ToList();
                    foreach (FieldContent content in contentList)
                    {
                        Type parentType = content.ParentEntity;
                        string realtionColumn = content.RealtionColumn;
                        object relationId = null;
                        if (fieldMap.TryGetValue(realtionColumn, out FieldContent relationContent))
                        {
                            relationId = relationContent.GetMethod.Invoke(entity, null);
                        }
                        else
                        {
                            throw new BaseSqlException("refrence column " + realtionColumn + " not exists!");
                        }
                        object parentVo = GetByIdSimple(parentType, relationId);
                        content.SetMethod.Invoke(entity, [parentVo]);
                    }
                }
            }
            IEnumerable<FieldContent> oneToManys = fields.Where(x => x.IsOneToMany);
            if (!oneToManys.IsNullOrEmpty())
            {
                foreach (V entity in retList)
                {
                    List<FieldContent> contentList = oneToManys.ToList();
                    foreach (FieldContent content in contentList)
                    {
                        Type subType = content.SubType;
                        object id = pkColumn.GetMethod.Invoke(entity, null);
                        if (EntityReflectUtils.GetRelationMap().TryGetValue(entityType, out Dictionary<Type, string> subEntityMaps) && subEntityMaps.TryGetValue(subType, out string relationColumn))
                        {
                            Dictionary<string, FieldContent> subFieldMap = EntityReflectUtils.GetFieldsMap(subType);
                            if (subFieldMap.TryGetValue(relationColumn, out FieldContent relationContent))
                            {
                                MethodInfo methodInfo = queryByFieldsMethod.MakeGenericMethod(subType);
                                object rsList = methodInfo.Invoke(this, [relationColumn, Constants.SqlOperator.EQ, new object[] { id }, null]);
                                content.SetMethod.Invoke(entity, [rsList]);
                            }
                            else
                            {
                                throw new BaseSqlException(" subColumn " + relationColumn + " is not defined in Model!");
                            }

                        }
                        else
                        {
                            throw new BaseSqlException(" type " + subType.Name + " is not defined subEntity as ManyToOne!");
                        }
                    }
                }
            }
        }
        private UpdateEntry GetCurrrentUpdateEntry()
        {
            WaitForRefreshTimer();
            if (requestChanges.TryGetValue(Thread.CurrentThread, out UpdateEntry entry))
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
        private async void WaitForRefreshTimer()
        {
            while (refreshTag.Get())
            {
                Log.Information("refesh thread running,Waiting");
                await Task.Delay(300).ConfigureAwait(false);
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
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entityType);
            IEnumerable<FieldContent> oneToManys = fields.Where(x => x.IsOneToMany && (x.Cascade == CascadeType.PERSIST || x.Cascade == CascadeType.MERGE || x.Cascade == CascadeType.ALL));
            if (!oneToManys.IsNullOrEmpty())
            {
                FieldContent pkColumn = EntityReflectUtils.GetPrimaryKey(superEntity.GetType());
                object id = pkColumn.GetMethod.Invoke(superEntity, null);

                foreach (FieldContent content in oneToManys.ToList())
                {
                    IList<dynamic> list = (IList<dynamic>)content.GetMethod.Invoke(superEntity, null);
                    string subColumn = content.RealtionColumn;
                    if (list != null && !list.IsNullOrEmpty())
                    {
                        Type subType = list.GetType().GetGenericArguments()[0];
                        Dictionary<string, FieldContent> subFieldMap = EntityReflectUtils.GetFieldsMap(subType);
                        if (!subFieldMap.TryGetValue(subColumn, out FieldContent realtionContent))
                        {
                            throw new BaseSqlException("refrence column " + subColumn + " not defined in entity " + subType.Name);
                        }
                        FieldContent subPkColumn = EntityReflectUtils.GetPrimaryKey(subType);
                        foreach (object voObj in list)
                        {
                            var sid = subPkColumn.GetMethod.Invoke(voObj, null);
                            BaseEntity subEntity = voObj as BaseEntity;
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
                    idsBuilder.Append(paramName).Append(",");
                    parameters[i] = GetDao().GetDialect().WrapParameter(i + 1, pkList[i]);
                }
            }
            string removeSql = removeBuilder.Append(idsBuilder.ToString()[..(idsBuilder.Length - 1)]).Append(")").ToString();

            effectRow += GetDao().Execute(command, removeSql, parameters);

            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(superEntityType);
            IEnumerable<FieldContent> oneToManys = fields.Where(x => x.IsOneToMany && (x.Cascade == CascadeType.REMOVE || x.Cascade == CascadeType.ALL));
            if (!oneToManys.IsNullOrEmpty())
            {
                foreach (FieldContent content in oneToManys.ToList())
                {
                    string subColumn = content.RealtionColumn;
                    Type subType = content.ParamType.GetGenericArguments()[0];
                    Dictionary<string, FieldContent> subFieldMap = EntityReflectUtils.GetFieldsMap(subType);
                    if (!subFieldMap.TryGetValue(subColumn, out FieldContent relationContent))
                    {
                        throw new BaseSqlException("refrence column " + subColumn + " not defined in entity " + subType.Name);
                    }
                    Tuple<StringBuilder, IList<DbParameter>> tuple1 = SqlUtils.GetRemoveCondition(GetDao(), subType, relationContent.FieldName, Constants.SqlOperator.IN, [pkList]);
                    effectRow += GetDao().Execute(command, tuple1.Item1.ToString(), tuple1.Item2.ToArray());
                }
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
                            while (!requestChanges.TryRemove(x, out _))
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
