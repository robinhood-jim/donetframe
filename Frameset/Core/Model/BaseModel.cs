
using Frameset.Core.Annotation;

namespace Frameset.Core.Model
{
    public class BaseModel : BaseEntity
    {
        [MappingField(field: "create_time", IfRequired = true)]
        public string CreateTime { get; set; }
        [MappingField(field: "modify_time", IfRequired = true)]
        public string ModifyTime { get; set; }
        [MappingField(field: "creator", IfRequired = true)]
        public long Creator { get; set; }
        [MappingField("modifier", IfRequired = true)]
        public long Modifier { get; set; }


    }
}