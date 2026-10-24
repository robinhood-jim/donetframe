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

        bool UpdateEntity<V, P>(V entity) where V : BaseEntity where P : notnull;

        int RemoveEntity<V, P>(List<P> pks) where V : BaseEntity where P : notnull;

        int RemoveLogic<V, P>(List<P> pks, string logicColumn, int status) where V : BaseEntity where P : notnull;

        V GetById<V, P>(P pk, bool ignoreLogic = false) where V : BaseEntity where P : notnull;
        List<Dictionary<string, object>> QueryBySql(string sql, object[] values);

        List<V> QueryModelsByField<V>(string propertyName, Constants.SqlOperator oper, object[] values, string? orderByStr = null) where V : BaseEntity;

        List<dynamic> QueryMapper(string nameSpace, string queryId, object queryObject, List<string>? inputParameters = null);
        int ExecuteMapper(string nameSpace, string exeId, object input);
        int Execute(string sql, DbParameter[] parameters);

        PageDTO<O> QueryPage<V, O>(PageQuery query) where V : BaseEntity;
        long InsertBatch<V>(IEnumerable<V> models, CancellationToken token) where V : BaseEntity;

        PageDTO<V> QueryModelsPage<V>(PageQuery query) where V : BaseEntity;

        void ChangeDs(string dsName);

        void RestoreDs();
        string GetDsName();
        List<V> QueryByCondition<V>(FilterCondition condition, PageQuery? query = null) where V : BaseEntity;
        List<O> QueryByCondition<V, O>(FilterCondition condition) where V : BaseEntity;

        List<O> QueryByFields<V, O>(QueryParameter queryParams) where V : BaseEntity;
        bool ExecuteOperation(Action<IAdoDao, DbCommand> action);
        List<O> QueryByNamedParameter<O>(string sql, Dictionary<string, object> nameParamter);
        void SaveChanges();
        int RemoveByFields<V, P>(string fieldName, Constants.SqlOperator sqlOperator, object[] values) where V : BaseEntity;
        void DoWithQuery(string sql, Dictionary<string, object> QueryParamter, Action<IDataReader> action);
        string GetContextName();
        void SetAutoCommit(bool autoCommit);
        void ResetCommitStatus();
        void ManyToOne(Type subType, string fieldName, Type parentType);
        void OneToMany(Type parentType, Type subType, string fieldName, string relationColumn, CascadeType cascadeType = CascadeType.DETACH);
        IAdoDao GetDao();
    }
}
