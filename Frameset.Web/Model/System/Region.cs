using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frameset.Web.Model.System
{
    [MappingEntity("t_region")]
    public class Region : BaseEntity
    {
        [MappingField(IfPrimary = true)]
        public string Code
        {
            get; set;
        } = string.Empty;
        public string Name
        {
            get; set;
        } = string.Empty;
        [MappingField("p_code")]
        public string Pcode
        {
            get; set;
        } = string.Empty;
        public int Level
        {
            get; set;
        }
    }
}
