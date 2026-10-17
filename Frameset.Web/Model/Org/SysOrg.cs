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
        public string OrgCode
        {
            get; set;
        } = string.Empty;
        public long EntId
        {
            get; set;
        }
        public string OrgType
        {
            get; set;
        }
        public long Pid
        {
            get; set;
        }
        [MappingField("tree_level")]
        public int Level
        {
            get; set;
        }
        [MappingField("tree_code")]
        public string Code
        {
            get; set;
        } = string.Empty;
        public string OrderNo
        {
            get; set;
        }
    }
}
