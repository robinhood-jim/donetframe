using Frameset.Core.Query;

namespace Frameset.WebMvc.QueryDto
{
    public class SysRoleQueryDto : PageQuery
    {
        public string Status
        {
            get; set;
        } = string.Empty;
        public string Code
        {
            get; set;
        } = string.Empty;
    }
}
