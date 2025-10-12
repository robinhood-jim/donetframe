using Frameset.Core.Dao.Meta;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Mapper.Segment;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using System;
using System.Collections.Generic;
using System.Data.Common;


namespace Frameset.Core.Dao
{
    public interface IJdbcDao
    {
        string GetConnectString();
        string GetDbTypeStr();
        
        
        bool SaveEntity(DbCommand command, BaseEntity model, InsertSegment segment);
        bool UpdateEntity(DbCommand command, BaseEntity entity, UpdateSegment segment);
        int Execute(DbCommand command, string sql, DbParameter[] parameters);

        AbstractSqlDialect GetDialect();
        
        int QueryByInt(DbCommand command);
        long QueryByLong(DbCommand command);
        IList<Dictionary<string, object>> QueryBySql(DbCommand command, object[] objects);
        PageDTO<V> QueryPage<V>(DbCommand command, PageQuery query);
        IList<V> QueryModelsBySql<V>(Type modelType, DbCommand command, IList<DbParameter> parameters);
        object QueryMapper(SqlSelectSegment sqlsegment, Dictionary<string, object> paramMap, string nameSpace, DbCommand command, object queryObject);

        string GetCurrentSchema();
        
    }
}