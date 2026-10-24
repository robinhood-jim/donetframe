using Frameset.Core.Annotation;

namespace Frameset.Web.Model.System
{
    [MappingEntity("t_sys_codeset")]
    public class SysCodeset : AbstractModel
    {
        [MappingField(ifPrimary: true, ifIncrement: true)]
        public long Id
        {
            get; set;
        }
        [MappingField("en_name")]
        public string Code
        {
            get; set;
        } = string.Empty;
        [MappingField("cn_name")]
        public string Name
        {
            get; set;
        } = string.Empty;
        [OneToMany(typeof(SysCode))]
        public List<SysCode> Selections
        {
            get; set;
        } = [];

    }
}
