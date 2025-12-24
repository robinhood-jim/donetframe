using System.Collections.Generic;

namespace Frameset.Core.Mapper
{
    public class MapperConfig
    {
        public Dictionary<string, string> SqlMap
        {
            get; set;
        } = [];
        public Dictionary<string, ResultMap> ReslutMap
        {
            get; set;
        } = [];
        public Dictionary<string, string> ScriptMap
        {
            get; set;
        } = [];
    }
}
