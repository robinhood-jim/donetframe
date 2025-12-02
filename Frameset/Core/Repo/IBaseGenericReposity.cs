using Frameset.Core.Common;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using System.Collections.Generic;
using System.Reflection;

namespace Frameset.Core.Repo
{
    public interface IBaseGenericReposity
    {
        bool SavenEntity(BaseEntity entity);
        bool UpdateEntity(BaseEntity entity);
        int RemoveEntity(IList<object> pks);
        int RemoveLogic(IList<object> pks, string logicColumn, int status);
        BaseEntity GetById(object pk);
        IList<Dictionary<string, object>> QueryBySql(string sql, object[] values);
        IList<BaseEntity> QueryModelsByField(PropertyInfo info, Constants.SqlOperator oper, object[] values, string orderByStr = null);
        IList<BaseEntity> QueryModelsByField(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = null);
        object QueryMapper(string nameSpace, string queryId, object queryObject);
        int ExecuteMapper(string nameSpace, string exeId, object input);
        PageDTO<O> QueryPage<O>(PageQuery query);
        int InsertBatch(IList<BaseEntity> models);
    }
}
