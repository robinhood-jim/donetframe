using Frameset.Core.Common;
using Frameset.Core.Utils;
using Frameset.Web.Model.Org;

namespace Frameset.WebMvc.Vo
{
    public class SysOrgVO
    {
        public long Id
        {
            get; set;
        }
        public string OrgName
        {
            get; set;
        } = string.Empty;
        public string OrgCode
        {
            get; set;
        } = string.Empty;
        public string Status
        {
            get; set;
        } = Constants.VALID;
        public string OrgAbbr
        {
            get; set;
        } = string.Empty;
        public long Pid
        {
            get; set;
        }
        public List<SysOrgVO> Children
        {
            get; internal set;
        } = [];
        public static SysOrgVO FromEntity(SysOrg sysOrg)
        {
            SysOrgVO vo = new();
            BeanUtils.CopyProperties(sysOrg, vo);
            return vo;
        }
    }
}
