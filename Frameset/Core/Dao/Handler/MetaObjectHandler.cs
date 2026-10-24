using Frameset.Core.Common;
using System.Diagnostics;
using System.Reflection;

namespace Frameset.Core.Dao.Handler
{
    public abstract class MetaObjectHandler : IMetaObjectHandler
    {
        public abstract void InsertFill(MetaObject metaObject);
        public abstract void UpdateFill(MetaObject metaObject);
        public void SetValue(string fieldName, object value, MetaObject metaObject)
        {
            if (value != null && metaObject.HasProperty(fieldName, out PropertyInfo? propertyInfo))
            {
                Trace.Assert(propertyInfo != null && propertyInfo.DeclaringType != null);
                metaObject.SetValue(fieldName, ConvertUtil.ParseByType(propertyInfo.DeclaringType, value));
            }
        }
        public bool ContainColumn(string fieldName, MetaObject metaObject)
        {
            return metaObject.HasProperty(fieldName, out _);
        }
        public object? GetValueByName(string fieldName, MetaObject metaObject)
        {
            return metaObject.HasGetter(fieldName) ? metaObject.GetValue(fieldName) : null;
        }

    }
}
