using Frameset.Core.Dao.Meta;
using Frameset.Core.Dao.Utils;
using Frameset.Core.FileSystem;
using Frameset.Core.Mapper.Segment;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;

namespace Frameset.Core.Dao
{
    public interface IAdoDao
    {
        string GetConnectString();
        string GetDbTypeStr();

        /// <summary>
        /// Single Table save
        /// </summary>
        /// <param name="command"></param>
        /// <param name="model"></param>
        /// <param name="segment"></param>
        /// <returns></returns>
        bool SaveEntity(DbCommand command, BaseEntity model, InsertSegment segment);
        /// <summary>
        /// Single Table Update
        /// </summary>
        /// <param name="command"></param>
        /// <param name="entity"></param>
        /// <param name="segment"></param>
        /// <returns></returns>
        bool UpdateEntity(DbCommand command, UpdateSegment segment);
        /// <summary>
        /// Execute sepcify Sql
        /// </summary>
        /// <param name="command"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        int Execute(DbCommand command, string sql, DbParameter[] parameters);

        ISqlDialect GetDialect();

        int QueryByInt(DbCommand command, List<DbParameter>? parameters = null);
        long QueryByLong(DbCommand command, List<DbParameter>? parameters = null);
        List<Dictionary<string, object>> QueryBySql(DbCommand command, object[] objects);
        /// <summary>
        /// Query native and return with list targetType
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="querySql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        object QueryBySql(Type targetType, string querySql, List<DbParameter> parameters);
        /// <summary>
        /// LINQ Queryable query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IQueryable<T> Query<T>() where T : BaseEntity;
        /// <summary>
        /// Execute pageQuery
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="command"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        PageDTO<V> QueryPage<V>(DbCommand command, PageQuery query);
        /// <summary>
        /// ORM Query return list modelType
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        List<V> QueryModelsBySql<V>(DbCommand command, List<DbParameter> parameters);
        List<BaseEntity> QueryModelsBySql(Type modelType, DbCommand command, List<DbParameter> parameters);
        /// <summary>
        /// Mybatis Like Query
        /// </summary>
        /// <param name="sqlsegment"></param>
        /// <param name="paramMap"></param>
        /// <param name="nameSpace"></param>
        /// <param name="command"></param>
        /// 
        /// <returns></returns>
        List<dynamic> QueryMapper(SqlSelectSegment sqlsegment, Dictionary<string, object> paramMap, string nameSpace, DbCommand command);
        string GetCurrentSchema();
        /// <summary>
        /// Query DataReader with Operation
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="obj"></param>
        /// <param name="action"></param>
        void DoWithQuery(string sql, object[] obj, Action<IDataReader> action);
        void DoWithQueryNamed(string sql, Dictionary<string, object> QueryParameters, Action<IDataReader> action);
        /// <summary>
        /// Query using FilterCondition
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="command"></param>
        /// <param name="condition"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        List<V> QueryByCondition<V>(DbCommand command, FilterCondition condition, PageQuery? query = null);
        /// <summary>
        /// Query single Table with Complex Condition( AND/OR),support new Column and GroupBy Having
        /// </summary>
        /// <typeparam name="O">Return Objects Type</typeparam>
        /// <param name="entityType">Query single Table Model</param>
        /// <param name="command">DbCommand </param>
        /// <param name="queryParams">Query Parameter Model</param>
        /// <returns></returns>
        List<O> QueryByFields<O>(Type entityType, DbCommand command, QueryParameter queryParams);
        List<O> QueryByNamedParameter<O>(DbCommand command, Dictionary<string, object> namedParamter);
        void GetSqlAndParameterByCondition<V>(FilterCondition condition, out StringBuilder builder, out Dictionary<string, object> queryParameter);
        /// <summary>
        /// Logic Delete Column support
        /// </summary>
        /// <param name="logicColumn"></param>
        void SetLogicColumn(string logicColumn);
        string GetLogicColumn();
        void SetValidValue(int value);
        int GetValidValue();
        void SetInvalidValue(int value);
        int GetInvalidValue();
        /// <summary>
        /// Insert Batch with BulkCopy
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="schema"></param>
        /// <param name="tableName"></param>
        /// <param name="metas"></param>
        /// <param name="models"></param>
        /// <param name="token"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        long InsertBatch(DbConnection connection, string schema, string tableName, List<DataSetColumnMeta> metas, IEnumerable<Dictionary<string, object>> models, CancellationToken token, int batchSize = 10000);
        /// <summary>
        /// Batch Insert using IDataAdpater
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="list"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        int BatchInsert<T>(DbConnection connection, List<T> list, int batchSize = 1000);

    }
}