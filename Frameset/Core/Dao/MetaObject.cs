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
                return fieldContent.GetMethod.Invoke(originObj, null);
            }
            return null;
        }
        public void SetValue(string fieldName, object fieldValue)
        {
            if (fieldDict.TryGetValue(fieldName, out FieldContent? fieldContent))
            {
                fieldContent.SetMethod.Invoke(originObj, [fieldValue]);
            }
        }
        public bool HasGetter(string columnName)
        {
            return fieldDict.TryGetValue(columnName, out FieldContent? fieldContent) && fieldContent.GetMethod != null && fieldContent.GetMethod.IsPublic;
        }
        public bool HasSetter(string columnName)
        {
            return fieldDict.TryGetValue(columnName, out FieldContent? fieldContent) && fieldContent.SetMethod != null && fieldContent.SetMethod.IsPublic;
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
