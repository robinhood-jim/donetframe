using Frameset.Core.Dao.Meta;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Mapper.Segment;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;


namespace Frameset.Core.Dao
{
    public interface IJdbcDao
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
        int Execute(DbCommand command, string sql, DbParameter[] parameters);

        AbstractSqlDialect GetDialect();

        int QueryByInt(DbCommand command, List<DbParameter> parameters = null);
        long QueryByLong(DbCommand command, List<DbParameter> parameters = null);
        IList<Dictionary<string, object>> QueryBySql(DbCommand command, object[] objects);
        PageDTO<V> QueryPage<V>(DbCommand command, PageQuery query);
        IList<V> QueryModelsBySql<V>(Type modelType, DbCommand command, IList<DbParameter> parameters = null);
        /// <summary>
        /// Mybatis Like Query
        /// </summary>
        /// <param name="sqlsegment"></param>
        /// <param name="paramMap"></param>
        /// <param name="nameSpace"></param>
        /// <param name="command"></param>
        /// <param name="queryObject"></param>
        /// <returns></returns>
        object QueryMapper(SqlSelectSegment sqlsegment, Dictionary<string, object> paramMap, string nameSpace, DbCommand command, object queryObject);

        string GetCurrentSchema();
        /// <summary>
        /// Query DataReader with Operation
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="obj"></param>
        /// <param name="action"></param>
        void DoWithQuery(string sql, object[] obj, Action<IDataReader> action);
        void DoWithQueryNamed(string sql, Dictionary<string, object> QueryParameters, Action<IDataReader> action);
        List<V> QueryByConditon<V>(DbCommand command, FilterCondition condition);
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
    }
}