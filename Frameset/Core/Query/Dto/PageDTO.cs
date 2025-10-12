using System.Collections.Generic;

namespace Frameset.Core.Query.Dto
{
    public class PageDTO<V>
    {
        public long Page { get; set; }
        public long Limit { get; set; }
        public long Count { get; set; }
        public string OrderField { get; set; }
        public long Total { get; set; }
        public IList<V> Results { get; set; } = new List<V>();
        public PageDTO(long total, long limit)
        {
            Total = total;
            Limit = limit;
            long pageSize = total / limit;
            if (total % limit > 0)
            {
                pageSize++;
            }
            Count = pageSize;
        }
        public PageDTO(long total, long limit, long page)
        {
            Total = total;
            Limit = limit;
            Page = page;
            long pageSize = total / limit;
            if (total % limit > 0)
            {
                pageSize++;
            }
            Count = pageSize;
        }
    }
}