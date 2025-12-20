using System.Collections.Generic;

namespace Frameset.Core.Dao.Utils
{
    public class QueryParameter
    {
        public string SelectColumns
        {
            get; set;
        }
        public Dictionary<string, object> NewColumns
        {
            get; set;
        }
        public string GroupBy
        {
            get; set;
        }
        public Dictionary<string, object> Having
        {
            get; set;
        }
        public string OrderBy
        {
            get; set;
        }
        public Dictionary<string, object> Parameters
        {
            get; set;
        }
    }
}
