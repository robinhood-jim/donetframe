using Frameset.Core.Annotation;

namespace Frameset.Web.Model.Org
{
    [MappingEntity("t_sys_enterprise")]
    public class Enterprise : AbstractModel
    {
        [MappingField(IfIncrement = true, IfPrimary = true)]
        public long Id
        {
            get; set;
        }
        public long OwnerId
        {
            get; set;
        }
        public long ManagerId
        {
            get; set;
        }
        public string Name
        {
            get; set;
        } = string.Empty;
        public string Code
        {
            get; set;
        } = string.Empty;
        public string TradeNo
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
        public string Industry
        {
            get; set;
        } = string.Empty;
        public string District
        {
            get; set;
        } = string.Empty;
        public string Address
        {
            get; set;
        } = string.Empty;

    }
}
