using Frameset.Core.Dao.Utils;
using System.Collections.Generic;

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
        public object GetValue(string columnName)
        {
            if (fieldDict.TryGetValue(columnName, out FieldContent fieldContent))
            {
                return fieldContent.GetMethod.Invoke(originObj, null);
            }
            return null;
        }
        public void SetValue(string fieldName, object fieldValue)
        {
            if (fieldDict.TryGetValue(fieldName, out FieldContent fieldContent))
            {
                fieldContent.SetMethod.Invoke(originObj, [fieldValue]);
            }
        }
        public bool HasGetter(string columnName)
        {
            return fieldDict.TryGetValue(columnName, out FieldContent fieldContent) && fieldContent.GetMethod != null;
        }
        public bool HasSetter(string columnName)
        {
            return fieldDict.TryGetValue(columnName, out FieldContent fieldContent) && fieldContent.SetMethod != null;
        }
    }
}
