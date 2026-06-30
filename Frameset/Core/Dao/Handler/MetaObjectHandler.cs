namespace Frameset.Core.Dao.Handler
{
    public abstract class MetaObjectHandler
    {
        public abstract void InsertFill(MetaObject metaObject);
        public abstract void UpdateFill(MetaObject metaObject);
        public void SetValueByName(string fieldName, object value, MetaObject metaObject)
        {
            if (value != null && metaObject.HasSetter(fieldName))
            {
                metaObject.SetValue(fieldName, value);
            }
        }
        public bool ContainColumn(string fieldName, MetaObject metaObject)
        {
            return metaObject.HasGetter(fieldName) && metaObject.HasSetter(fieldName);
        }
        public object getValueByName(string fieldName, MetaObject metaObject)
        {
            return metaObject.HasGetter(fieldName) ? metaObject.GetValue(fieldName) : null;
        }

    }
}
