namespace Frameset.Core.Dao.Handler
{
    public interface IMetaObjectHandler
    {
        void InsertFill(MetaObject metaObject);
        void UpdateFill(MetaObject metaObject);
        void SetValue(string fieldName, object value, MetaObject metaObject);
        bool ContainColumn(string fieldName, MetaObject metaObject);
        object? GetValueByName(string fieldName, MetaObject metaObject);
    }
}
