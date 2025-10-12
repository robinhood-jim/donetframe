using System;
using System.Collections.Generic;
using System.Reflection;

namespace Frameset.Core.Mapper
{
    public class ResultMap
    {
        public ResultMap(string modelType)
        {
            int pos = modelType.IndexOf(".");
            if (pos != -1)
            {
                this.ModelType = Assembly.Load(modelType.Substring(0, pos)).GetType(modelType);
            }
        }
        public Type ModelType
        {
            get; set;
        }
        public Dictionary<string, string> MappingColumns
        {
            get; internal set;
        } = new Dictionary<string, string>();

    }
}
