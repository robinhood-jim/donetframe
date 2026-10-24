using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Model;

namespace Frameset.Web.Model.Org
{
    [MappingEntity("t_tenant_user_invite")]
    public class TenantUserInvite : BaseModel
    {
        [MappingField(IfIncrement = true, IfPrimary = true)]
        public long Id
        {
            get; set;
        }
        public long TenantId
        {
            get; set;
        }
        public long EmpId
        {
            get; set;
        }
        public string Phone
        {
            get; set;
        } = string.Empty;
        public string InviteCode
        {
            get; set;
        } = string.Empty;
        public long CreatorId
        {
            get; set;
        }
        public DateTime CreateTm
        {
            get; set;
        }
        public string Status
        {
            get; set;
        } = Constants.INVALID;
        public DateTime? AcceptTime
        {
            get; set;
        }

    }
}
