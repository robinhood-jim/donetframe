using Frameset.Core.Utils;
using System;
using System.Collections.Generic;

namespace Frameset.Core.Mapper
{
    public class ResultMap
    {
        public ResultMap(string modelType)
        {
            if (string.Equals(modelType, "Map", StringComparison.OrdinalIgnoreCase))
            {
                ModelType = typeof(Dictionary<string, object>);
            }
            else
            {
                Type? type = ClassUtils.GetUserType(modelType);
                if (type != null)
                {
                    ModelType = type;
                }
            }
        }
        public Type? ModelType
        {
            get; set;
        }
        public Dictionary<string, string> MappingColumns
        {
            get; internal set;
        } = [];

    }
}
