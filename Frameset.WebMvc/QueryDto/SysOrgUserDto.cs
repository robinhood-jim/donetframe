using Frameset.Core.Query;

namespace Frameset.WebMvc.QueryDto
{
    public class SysOrgUserDto : PageQuery
    {
        public long Pid
        {
            get; set;
        }
        public string SelType
        {
            get; set;
        } = string.Empty;
    }
}
