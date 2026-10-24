using System.Collections.Generic;

namespace Frameset.Core.Dao.Utils
{
    public class QueryParameter
    {
        public string SelectColumns
        {
            get; set;
        } = null!;

        public Dictionary<string, object> NewColumns
        {
            get;
            set;
        } = [];
        public string GroupBy
        {
            get; set;
        } = null!;
        public Dictionary<string, object> Having
        {
            get; set;
        } = null!;
        public string OrderBy
        {
            get; set;
        } = null!;
        public Dictionary<string, object> Parameters
        {
            get; set;
        } = null!;
    }
}
