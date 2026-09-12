using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Model;

namespace Frameset.Web.Model.System;

[MappingEntity("t_sys_resource_info")]
public class SysResource : BaseEntity
{
    [MappingField(ifPrimary: true, ifIncrement: true)]
    public long Id
    {
        get;
        set;
    }
    [MappingField(field: "res_name")]
    public string Name
    {
        get;
        set;
    } = string.Empty;
    public string ResType
    {
        get;
        set;
    } = string.Empty;

    public string Url
    {
        get;
        set;
    } = string.Empty;

    public long PowerId
    {
        get;
        set;
    }
    [MappingField(field: "is_leaf")]
    public int LeafTag
    {
        get;
        set;
    }
    [MappingField(field: "res_code")]
    public string Code
    {
        get;
        set;
    } = string.Empty;

    public string ResId
    {
        get;
        set;
    } = string.Empty;

    public long Pid
    {
        get;
        set;
    }

    public int SeqNo
    {
        get;
        set;
    }

    public string Remark
    {
        get;
        set;
    } = string.Empty;

    public long OrgId
    {
        get;
        set;
    }

    public string Permission
    {
        get;
        set;
    } = string.Empty;

    public long TenantId
    {
        get;
        set;
    }

    public string RouterPath
    {
        get;
        set;
    } = string.Empty;
    public string Icon
    {
        get; set;
    } = string.Empty;
    [LogicColumn]
    public string Status
    {
        get; set;
    } = Constants.VALID;
}