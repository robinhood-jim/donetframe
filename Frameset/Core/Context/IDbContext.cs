using Frameset.Core.Annotation;
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

namespace Frameset.Core.Context
{
    public interface IDbContext : IDisposable
    {
        /// <summary>
        /// Save Single Entity
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool SaveEntity<V>(V entity) where V : BaseEntity;

        bool UpdateEntity<V, P>(V entity) where V : BaseEntity;

        int RemoveEntity<V, P>(IList<P> pks) where V : BaseEntity;

        int RemoveLogic<V, P>(IList<P> pks, string logicColumn, int status) where V : BaseEntity;

        V GetById<V, P>(P pk) where V : BaseEntity;
        IList<Dictionary<string, object>> QueryBySql(string sql, object[] values);

        IList<V> QueryModelsByField<V>(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null) where V : BaseEntity;

        object QueryMapper(string nameSpace, string queryId, object queryObject);
        int ExecuteMapper(string nameSpace, string exeId, object input);

        PageDTO<O> QueryPage<V, O>(PageQuery query) where V : BaseEntity;
        long InsertBatch<V>(IEnumerable<V> models, CancellationToken token) where V : BaseEntity;

        PageDTO<V> QueryModelsPage<V>(PageQuery query) where V : BaseEntity;

        void ChangeDs(string dsName);

        void RestoreDs();
        string GetDsName();
        List<O> QueryByCondtion<V, O>(FilterCondition condition) where V : BaseEntity;

        List<O> QueryByFields<V, O>(QueryParameter queryParams) where V : BaseEntity;
        bool ExecuteOperation(Action<IJdbcDao, DbCommand> action);
        List<O> QueryByNamedParameter<O>(string sql, Dictionary<string, object> nameParamter);
        void SaveChanges();
        int RemoveByFields<V, P>(string fieldName, Constants.SqlOperator sqlOperator, object[] values) where V : BaseEntity;
        void DoWithQuery(string sql, Dictionary<string, object> QueryParamter, Action<IDataReader> action);
        string GetContextName();
        void SetAutoCommit(bool autoCommit);
        void ResetCommitStatus();
        void ManyToOne(Type subType, string fieldName, Type parentType);
        void OneToMany(Type parentType, Type subType, string fieldName, string relationColumn, CascadeType cascadeType);
    }
}
