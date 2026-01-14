using Frameset.Core.Common;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

namespace Frameset.Core.Repo
{
    public interface IBaseRepository<V, P> where V : BaseEntity
    {
        /// <summary>
        /// Single Table Insert
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool SaveEntity(V entity, Action<V> insertBeforeAction = null, Func<DbCommand, V, bool> insertAfterAction = null);
        /// <summary>
        /// Single Table Update
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool UpdateEntity(V entity, Action<V> updateBeforeAction = null, Func<DbCommand, V, bool> updateAfterAction = null);
        /// <summary>
        /// Single Table Delete
        /// </summary>
        /// <param name="pks"></param>
        /// <returns></returns>
        int RemoveEntity(IList<P> pks);
        /// <summary>
        /// Single Table Delete Logic
        /// </summary>
        /// <param name="pks"></param>
        /// <param name="logicColumn"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        int RemoveLogic(IList<P> pks, string logicColumn, int status);
        /// <summary>
        /// Get By Primary Key
        /// </summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        V GetById(P pk);
        /// <summary>
        /// Query By Sql
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="values">Parameter as @1 @2</param>
        /// <returns></returns>
        IList<Dictionary<string, object>> QueryBySql(string sql, object[] values);
        /// <summary>
        /// Query By Single Parameter
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="oper"></param>
        /// <param name="values"></param>
        /// <param name="orderByStr"></param>
        /// <returns></returns>
        IList<V> QueryModelsByField(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null);
        /// <summary>
        /// Query With Mybatis xml config
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <param name="queryId"></param>
        /// <param name="queryObject"></param>
        /// <returns></returns>
        object QueryMapper(string nameSpace, string queryId, object queryObject);
        int ExecuteMapper(string nameSpace, string exeId, object input);
        /// <summary>
        /// Query Page
        /// </summary>
        /// <typeparam name="O"></typeparam>
        /// <param name="query"></param>
        /// <returns>DTO or dictionary </returns>
        PageDTO<O> QueryPage<O>(PageQuery query);
        long InsertBatch(IEnumerable<V> models, CancellationToken token);
        /// <summary>
        /// Query Page return List models
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        PageDTO<V> QueryModelsPage(PageQuery query);
        /// <summary>
        /// Change DataSource Temporary with ThreadLocal
        /// </summary>
        /// <param name="dsName">change DataSource Name</param>
        void ChangeDs(string dsName);
        /// <summary>
        /// Restore Temporary DataSource Swith
        /// </summary>
        void RestoreDs();
        string GetDsName();
        List<O> QueryByCondtion<O>(FilterCondition condition);
        /// <summary>
        /// Query single Table with Complex Condition( AND/OR),support new Column and GroupBy Having
        /// </summary>
        /// <typeparam name="O">Return Object </typeparam>
        /// <param name="queryParams">Query Parameter Object</param>
        /// <returns></returns>
        List<O> QueryByFields<O>(QueryParameter queryParams);
        bool ExecuteOperation(Action<IJdbcDao, DbCommand> action);
        List<O> QueryByNamedParameter<O>(string sql, Dictionary<string, object> nameParamter);
    }
}
