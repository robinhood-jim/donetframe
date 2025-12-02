using System.Collections.Generic;

namespace Frameset.Core.Query
{
    public class PageQuery
    {
        public long PageSize { get; set; } = 10;
        public long CurrentPage { get; set; } = 1;
        public string QueryId { get; set; }
        public string NameSpace { get; set; }
        public long PageCount { get; set; } = 0;
        public string OrderField { get; set; }
        public bool OrderAsc { get; set; } = false;
        public string Order { get; set; }
        public long Total { get; set; }
        public string QuerySql { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> MappingColumns { get; set; } = new Dictionary<string, string>();
        public PageQuery()
        {

        }
        public PageQuery(long pageSize)
        {
            this.PageSize = pageSize;

        }
        public PageQuery(long pageSize, long currentPage)
        {
            this.PageSize = pageSize;
            this.CurrentPage = currentPage;
        }
    }
}