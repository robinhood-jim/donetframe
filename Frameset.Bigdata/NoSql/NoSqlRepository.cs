using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.FileSystem;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Spring.Util;

namespace Frameset.Bigdata.NoSql
{
    public abstract class NoSqlRepository<V, P> : INoSqlRepository<V, P> where V : BaseEntity
    {
        protected EntityContent content;
        protected FieldContent pkColumn;
        protected readonly Type entityType;
        protected readonly Type pkType;
        protected IList<FieldContent> fieldContents;
        protected Dictionary<string, FieldContent> fieldMap;
        public NoSqlRepository(DataCollectionDefine define)
        {
            Type[] genericType = GetType().GetInterfaces()[0].GetGenericArguments();
            AssertUtils.IsTrue(genericType[0].IsSubclassOf(typeof(BaseEntity)));
            content = EntityReflectUtils.GetEntityInfo(genericType[0]);
            fieldContents = EntityReflectUtils.GetFieldsContent(genericType[0]);
            fieldMap = EntityReflectUtils.GetFieldsMap(genericType[0]);
            pkColumn = fieldContents.First(x => x.IfPrimary);
            entityType = genericType[0];
            pkType = genericType[1];
        }


        public abstract V GetById(P pk);


        public virtual IList<Dictionary<string, object>> QueryBySql(string sql, object[] values)
        {
            throw new NotImplementedException();
        }

        public virtual IList<V> QueryModelsByField(string propertyName, Constants.SqlOperator oper, object[] values, string orderByStr = "")
        {
            throw new NotImplementedException();
        }

        public virtual PageDTO<V> QueryModelsPage(PageQuery query)
        {
            throw new NotImplementedException();
        }


        public abstract int RemoveEntity(IList<P> pks);

        public abstract bool SaveEntity(V entity);

        public abstract bool UpdateEntity(V entity);


    }
}
