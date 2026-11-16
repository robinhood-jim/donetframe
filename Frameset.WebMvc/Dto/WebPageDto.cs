using Frameset.Core.Query.Dto;

namespace Frameset.WebMvc.Dto
{
    public class WebPageDto<V>
    {
        public long CurrentPage
        {
            get; set;
        }
        public long Pages
        {
            get; set;
        }
        public long PageSize
        {
            get; set;
        }
        public long Total
        {
            get; set;
        }
        public List<V> Records
        {
            get; set;
        } = [];
        public static WebPageDto<V> FromPageDto(PageDTO<V> pageDTO)
        {
            WebPageDto<V> dto = new();
            dto.Pages = pageDTO.Count;
            dto.Total = pageDTO.Total;
            dto.PageSize = pageDTO.Limit;
            dto.CurrentPage = pageDTO.Page;
            dto.Records = pageDTO.Results;
            return dto;
        }
        public WebPageDto()
        {

        }
        public WebPageDto(long total, long limit, long current, List<V> results)
        {
            Total = total;
            CurrentPage = current;
            PageSize = limit;
            Pages = total / limit;
            if (total % limit > 0)
            {
                Pages++;
            }
            Records = results;

        }
    }
}
