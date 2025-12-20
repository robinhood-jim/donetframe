using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using System.Collections.Generic;
using System.Threading;

namespace Frameset.Core.Repo
{
    public interface IBaseRepository<V, P> where V : BaseEntity
    {
        bool SaveEntity(V entity);
        bool UpdateEntity(V entity);
        int RemoveEntity(IList<P> pks);
        int RemoveLogic(IList<P> pks, string logicColumn, int status);
        V GetById(P pk);
        IList<Dictionary<string, object>> QueryBySql(string sql, object[] values);
        IList<V> QueryModelsByField(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null);
        object QueryMapper(string nameSpace, string queryId, object queryObject);
        int ExecuteMapper(string nameSpace, string exeId, object input);
        PageDTO<O> QueryPage<O>(PageQuery query);
        long InsertBatch(IEnumerable<V> models, CancellationToken token);
        PageDTO<V> QueryModelsPage(PageQuery query);
        void ChangeDs(string dsName);
        void RestoreDs();
        string GetDsName();
        List<O> QueryByCondtion<O>(FilterCondition condition);
        List<O> QueryByFields<O>(QueryParameter queryParams);
    }
}
