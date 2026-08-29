using Frameset.Core.Annotation;
using Frameset.Core.Common;

namespace Frameset.Web.Model.System;

public class SysResourceRole : AbstractModel
{
    [MappingField(IfIncrement = true,IfPrimary = true)]
    public long Id
    {
        get;
        set;
    }
    public long RoleId
    {
        get;
        set;
    }

    public long ResId
    {
        get;
        set;
    }
    [LogicColumn]
    public string Status
    {
        get;
        set;
    } = Constants.VALID;

}