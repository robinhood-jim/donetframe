using Frameset.Core.Annotation;

namespace Frameset.Web.Model.System;

public class SysResourceRole : AbstractModel
{
    [MappingField(IfIncrement = true, IfPrimary = true)]
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


}