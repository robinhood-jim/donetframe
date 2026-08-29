using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model.Org
{
    [MappingEntity("t_teanat_info")]
    public class TenantInfo : BaseEntity
    {
        [MappingField(ifPrimary: true, ifIncrement: true)]
        public long Id
        {
            get; set;
        }
        public string TeanatName
        {
            get; set;
        }
        public string TeanatCode
        {
            get; set;
        }
        public long OrgId
        {
            get; set;
        }
        public string Logo
        {
            get; set;
        }
        public DateTime RegTime
        {
            get; set;
        }
        public DateTime AuditTime
        {
            get; set;
        }

    }
}
