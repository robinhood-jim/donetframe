
using Frameset.Core.Annotation;
using Frameset.Core.Common;

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
        [MappingField(field: "modifier", IfRequired = true)]
        public long Modifier { get; set; }
        [MappingField(field: "status")]
        public string Status
        {
            get; set;
        } = Constants.VALID;

    }
}