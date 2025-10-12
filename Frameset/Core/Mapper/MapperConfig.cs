using System.Collections.Generic;

namespace Frameset.Core.Mapper
{
    public class MapperConfig
    {
        public Dictionary<string, string> SqlMap
        {
            get; set;
        } = new Dictionary<string, string>();
        public Dictionary<string, ResultMap> ReslutMap
        {
            get; set;
        } = new Dictionary<string, ResultMap>();
        public Dictionary<string, string> ScriptMap
        {
            get; set;
        } = new Dictionary<string, string>();
    }
}
