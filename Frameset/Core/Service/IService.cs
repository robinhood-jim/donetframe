using Frameset.Core.Common;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace Frameset.Core.Service
{
    public interface IService<V, P> where V : BaseEntity where P : notnull
    {
        bool SaveEntity(V entity);
        bool UpdateEntity(V entity);
        List<V> QueryModeslByField(string fieldName, Constants.SqlOperator sqlOperator, object[] values);
        List<Dictionary<string, object>> QueryBySql(string sql, object[] values);
        V GetById(P id, bool ignoreLogic = false);
        int RemoveEntity(List<P> ids);
        List<dynamic> QueryMapper(string nameSpace, string queryId, object queryObject);
        int ExecuteMapper(string nameSpace, string queryId, object queryObject);
        PageDTO<O> QueryPage<O>(PageQuery query);
        PageDTO<V> QueryModelsPage(PageQuery query);
        List<V> QueryByCondition(FilterCondition condition, PageQuery query);
        List<O> QueryByCondition<O>(FilterCondition condition);
        int RemoveByFields(string fieldName, Constants.SqlOperator sqlOperator, object[] values);
        long InsertBatch(IEnumerable<V> models, CancellationToken token);
        void DoWithQuery(string sql, Dictionary<string, object> QueryParamter, Action<IDataReader> action);
        void ChangeDs(string dsName);
        void RestoreDs();
        string GetDsName();
        void SetAutoCommit(bool autoCommit);
        void ResetCommitStatus();
        int Execute(string sql, DbParameter[] parameters);
        IAdoDao GetDao();
    }
}
