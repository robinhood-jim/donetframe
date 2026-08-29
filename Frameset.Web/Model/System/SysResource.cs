using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model.System;

[MappingEntity("t_sys_resource")]
public class SysResource : BaseEntity
{
    [MappingField(ifPrimary:true,ifIncrement:true)]
    public long Id
    {
        get;
        set;
    }
    [MappingField(field:"res_name")]
    public string Name
    {
        get;
        set;
    }
    public string ResType
    {
        get;
        set;
    }

    public string Url
    {
        get;
        set;
    }

    public long PowerId
    {
        get;
        set;
    }
    [MappingField(field:"is_leaf")]
    public int LeafTag
    {
        get;
        set;
    }
    [MappingField(field:"res_code")]
    public string Code
    {
        get;
        set;
    }

    public string ResId
    {
        get;
        set;
    }

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
    }

    public long OrgId
    {
        get;
        set;
    }
    
    public string Permission
    {
        get;
        set;    
    }

    public long TenantId
    {
        get;
        set;
    }

    public string RouterPath
    {
        get;
        set;
    }
}