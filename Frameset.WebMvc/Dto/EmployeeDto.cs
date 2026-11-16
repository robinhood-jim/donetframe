namespace Frameset.WebMvc.Dto
{
    public class EmployeeDto
    {
        public long Id
        {
            get; set;
        }
        public string Name
        {
            get; set;
        } = string.Empty;
        public string UserAccount
        {
            get; set;
        } = string.Empty;
        public string ContactPhone
        {
            get; set;
        } = string.Empty;
        public string Address
        {
            get; set;
        } = string.Empty;
        public string Gender
        {
            get; set;
        } = string.Empty;
        public string District
        {
            get; set;
        } = string.Empty;
        public string CreditNo
        {
            get; set;
        } = string.Empty;
        public string BrithDay
        {
            get; set;
        } = string.Empty;

        public long UserId
        {
            get; set;
        }
        public string VerifyCode
        {
            get; set;
        } = string.Empty;
    }
}
