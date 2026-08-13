using Frameset.Core.Common;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;

namespace Frameset.Bigdata.NoSql
{
    public interface INoSqlRepository<V, P> where V : BaseEntity
    {
        V GetById(P pk);
        IList<Dictionary<string, object>> QueryBySql(string sql, object[] values);
        bool SaveEntity(V entity);
        bool UpdateEntity(V entity);
        int RemoveEntity(IList<P> pks);
        IList<V> QueryModelsByField(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = "");
        PageDTO<V> QueryModelsPage(PageQuery query);
    }
}
