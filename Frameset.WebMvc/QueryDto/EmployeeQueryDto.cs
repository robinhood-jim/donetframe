using Frameset.Core.Query;

namespace Frameset.WebMvc.QueryDto
{
    public class EmployeeQueryDto : PageQuery
    {
        public string Phone
        {
            get; set;
        } = string.Empty;
        public string District
        {
            get; set;
        } = string.Empty;
        public string Name
        {
            get; set;
        } = string.Empty;
    }
}
