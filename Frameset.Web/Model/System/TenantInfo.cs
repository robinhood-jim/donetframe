using Frameset.Core.Annotation;


namespace Frameset.Web.Model.System
{
    [MappingEntity("t_tenant_info")]
    public class TenantInfo : AbstractModel
    {
        public long Id
        {
            get; set;
        }
        public string TenantName
        {
            get; set;
        } = string.Empty;
        public string TenantCode
        {
            get; set;
        } = string.Empty;
        public long OrgId
        {
            get; set;
        }
        public string Logo
        {
            get; set;
        } = string.Empty;
        public int Level
        {
            get; set;
        }
        public string Description
        {
            get; set;
        } = string.Empty;
        public DateTime RegTime
        {
            get; set;
        }
        public DateTime? AuditTime
        {
            get; set;
        }


    }
}
