using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model.Org
{
    [MappingEntity("t_sys_org_info")]
    public class SysOrg : BaseEntity
    {
        [MappingField(ifIncrement: true, ifPrimary: true)]
        public long Id
        {
            get; set;
        }
        public string OrgName
        {
            get; set;
        } = string.Empty;
        public string OrgAbbr
        {
            get; set;
        } = string.Empty;
        private string OrgCode
        {
            get; set;
        } = string.Empty;
    }
}
