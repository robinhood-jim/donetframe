namespace Frameset.WebMvc.Dto
{
    public class SysResourceDto
    {
        public long Id
        {
            get; set;
        }
        public long Pid
        {
            get; set;
        }
        public string ResName
        {
            get; set;
        } = string.Empty;
        public string ResType
        {
            get; set;
        } = string.Empty;
        public string Icon
        {
            get; set;
        } = string.Empty;
        public string RouterPath
        {
            get; set;
        } = string.Empty;
        public int Sort
        {
            get; set;
        }
        public string ComponentPath
        {
            get; set;
        } = string.Empty;
        public string Permission
        {
            get;
            set;
        } = string.Empty;
        public List<SysResourceDto> Children
        {
            get; set;
        } = [];
    }
}
