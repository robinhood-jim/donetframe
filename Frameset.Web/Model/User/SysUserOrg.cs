using Frameset.Core.Annotation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frameset.Web.Model.User
{
    [MappingEntity("t_sys_user_org_r")]
    public class SysUserOrg : AbstractModel
    {
        [MappingField(IfPrimary =true,IfIncrement =true)]
        public long Id
        {
            get;set;
        }
        public long OrgId
        {
            get;set;
        }
        public long UserId
        {
            get;set;
        }

    }
}
