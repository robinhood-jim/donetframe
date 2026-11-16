using Frameset.Core.Dao.Utils;
using System.Collections.Generic;
using System.Reflection;

namespace Frameset.Core.Dao
{
    public class MetaObject
    {
        private readonly object originObj;
        private readonly Dictionary<string, FieldContent> fieldDict;
        public MetaObject(object originObj, Dictionary<string, FieldContent> fieldDict)
        {
            this.originObj = originObj;
            this.fieldDict = fieldDict;
        }
        public object GetOriginObj()
        {
            return originObj;
        }
        public object? GetValue(string columnName)
        {
            if (fieldDict.TryGetValue(columnName, out FieldContent? fieldContent))
            {
                return fieldContent.Getter(originObj);
            }
            return null;
        }
        public void SetValue(string fieldName, object fieldValue)
        {
            if (fieldDict.TryGetValue(fieldName, out FieldContent? fieldContent))
            {
                fieldContent.Setter(originObj, fieldValue);
            }
        }
        public bool HasGetter(string columnName)
        {
            return fieldDict.TryGetValue(columnName, out FieldContent? fieldContent) && fieldContent.Getter!=null;
        }
        public bool HasSetter(string columnName)
        {
            return fieldDict.TryGetValue(columnName, out FieldContent? fieldContent) && fieldContent.Setter!=null;
        }
        public bool HasProperty(string columnName, out PropertyInfo? propertyInfo)
        {
            propertyInfo = null;
            if (fieldDict.TryGetValue(columnName, out FieldContent? fieldContent) && fieldContent.PropertyInfo != null)
            {
                propertyInfo = fieldContent.PropertyInfo;
                return true;
            }
            return false;
        }
    }
}
