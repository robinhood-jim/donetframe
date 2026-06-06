
using Frameset.Core.Annotation;
using System;

namespace Frameset.Core.Model
{
    public class BaseModel : BaseEntity
    {
        [MappingField(field: "create_time", IfRequired = true)]
        public DateTime CreateTime { get; set; }
        [MappingField(field: "modify_time", IfRequired = true)]
        public DateTime ModifyTime { get; set; }
        [MappingField(field: "creator", IfRequired = true)]
        public long Creator { get; set; }
        [MappingField(field: "modifier", IfRequired = true)]
        public long Modifier { get; set; }
        [MappingField(field: "status")]
        [LogicColumn]
        public int? Status
        {
            get; set;
        }

    }
}