using System;
using System.Collections.Generic;
using System.Reflection;

namespace Frameset.Core.Mapper
{
    public class ResultMap
    {
        public ResultMap(string modelType)
        {
            ModelType = Assembly.GetEntryAssembly()?.GetType(modelType);
        }
        public Type ModelType
        {
            get; set;
        }
        public Dictionary<string, string> MappingColumns
        {
            get; internal set;
        } = [];

    }
}
