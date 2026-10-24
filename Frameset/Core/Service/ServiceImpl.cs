using Frameset.Core.Common;
using Frameset.Core.Context;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;

namespace Frameset.Core.Service
{
    public class ServiceImpl<V, P> : IService<V, P> where V : BaseEntity where P : notnull
    {
        internal string dsName = "core";
        protected readonly IDbContext dbContext;
        public ServiceImpl(string dsName = "core")
        {
            this.dsName = dsName;
            dbContext = DbContextFactory.GetContext(dsName);
        }
        public bool SaveEntity(V entity)
        {
            Trace.Assert(entity != null, "");
            return dbContext.SaveEntity<V>(entity);
        }
        public bool UpdateEntity(V entity)
        {
            Trace.Assert(entity != null, "");
            return dbContext.UpdateEntity<V, P>(entity);
        }
        public List<V> QueryModeslByField(string fieldName, Constants.SqlOperator sqlOperator, object[] values)
        {
            return dbContext.QueryModelsByField<V>(fieldName, sqlOperator, values);
        }
        public List<Dictionary<string, object>> QueryBySql(string sql, object[] values)
        {
            return dbContext.QueryBySql(sql, values);
        }
        public V GetById(P id, bool ignoreLogic = false)
        {
            return dbContext.GetById<V, P>(id, ignoreLogic);
        }
        public int RemoveEntity(List<P> ids)
        {
            return dbContext.RemoveEntity<V, P>(ids);
        }
        public List<dynamic> QueryMapper(string nameSpace, string queryId, object queryObject, List<string>? inputParameters = null)
        {
            return dbContext.QueryMapper(nameSpace, queryId, queryObject, inputParameters);
        }
        public int ExecuteMapper(string nameSpace, string queryId, object queryObject)
        {
            return dbContext.ExecuteMapper(nameSpace, queryId, queryObject);
        }
        public PageDTO<O> QueryPage<O>(PageQuery query)
        {
            return dbContext.QueryPage<V, O>(query);
        }
        public PageDTO<V> QueryModelsPage(PageQuery query)
        {
            return dbContext.QueryModelsPage<V>(query);
        }
        public List<V> QueryByCondition(FilterCondition condition, PageQuery? query = null)
        {
            return dbContext.QueryByCondition<V>(condition, query);
        }
        public List<O> QueryByCondition<O>(FilterCondition condition)
        {
            return dbContext.QueryByCondition<V, O>(condition);
        }
        public long InsertBatch(IEnumerable<V> models, CancellationToken token)
        {
            return dbContext.InsertBatch<V>(models, token);
        }
        public void DoWithQuery(string sql, Dictionary<string, object> QueryParamter, Action<IDataReader> action)
        {
            dbContext.DoWithQuery(sql, QueryParamter, action);
        }
        public int Execute(string sql, DbParameter[] parameters)
        {
            return dbContext.Execute(sql, parameters);
        }
        public void ChangeDs(string dsName)
        {
            dbContext.ChangeDs(dsName);
        }
        public void RestoreDs()
        {
            dbContext.RestoreDs();
        }
        public string GetDsName()
        {
            return dbContext.GetDsName();
        }
        public int RemoveByFields(string fieldName, Constants.SqlOperator sqlOperator, object[] values)
        {
            return dbContext.RemoveByFields<V, P>(fieldName, sqlOperator, values);
        }
        public void SetAutoCommit(bool autoCommit)
        {
            dbContext.SetAutoCommit(autoCommit);
        }
        public void ResetCommitStatus()
        {
            dbContext.ResetCommitStatus();
        }
        public IAdoDao GetDao()
        {
            return dbContext.GetDao();
        }
    }
}
