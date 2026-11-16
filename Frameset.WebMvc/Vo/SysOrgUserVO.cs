namespace Frameset.WebMvc.Vo
{
    public class SysOrgUserVO
    {
        public long Id
        {
            get; set;
        }
        public string Name
        {
            get; set;
        } = string.Empty;
        public DateTime? BrithDay
        {
            get; set;
        }
        public string CreditNo
        {
            get; set;
        } = string.Empty;
        public string ContactPhone
        {
            get; set;
        } = string.Empty;
        public int Gender
        {
            get; set;
        }
        public string District
        {
            get; set;
        } = string.Empty;
        public string Address
        {
            get; set;
        } = string.Empty;
        public long RegOrgId
        {
            get; set;
        }
        public long UserId
        {
            get; set;
        }
        public string Province
        {
            get; set;
        } = string.Empty;
        public string City
        {
            get; set;
        } = string.Empty;
    }
}
