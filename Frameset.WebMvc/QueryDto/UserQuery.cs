using Frameset.Core.Query;

namespace Frameset.WebMvc.QueryDto
{
    public class UserQuery : PageQuery
    {
        public string Name
        {
            get; set;
        } = string.Empty;
        public string Phone
        {
            get; set;
        } = string.Empty;
        public bool Status
        {
            get; set;
        } = true;
        public string AccountType
        {
            get; set;
        } = string.Empty;
    }
}
