using System.Collections.Generic;

namespace Frameset.Core.Query
{
    public class PageQuery
    {
        public long PageSize { get; set; } = 10;
        public long CurrentPage { get; set; } = 1;
        public string QueryId { get; set; } = string.Empty;
        public string NameSpace { get; set; } = string.Empty;
        public long PageCount { get; set; } = 0;
        public string OrderField { get; set; } = null!;
        public bool OrderAsc { get; set; } = false;
        public string Order { get; set; } = string.Empty;
        public long Total { get; set; }
        public string QuerySql { get; set; } = string.Empty;
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