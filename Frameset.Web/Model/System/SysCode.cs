using Frameset.Core.Annotation;

namespace Frameset.Web.Model.System
{
    [MappingEntity("t_sys_code")]
    public class SysCode : AbstractModel
    {
        [MappingField(ifPrimary: true, ifIncrement: true)]
        public long Id
        {
            get; set;
        }
        public long CsId
        {
            get; set;
        }
        [MappingField("item_name")]
        public string Name
        {
            get; set;
        } = string.Empty;
        [MappingField("item_value")]
        public string Value
        {
            get; set;
        } = string.Empty;
        [ManyToOne(typeof(SysCodeset), "CsId")]
        public SysCodeset Codeset
        {
            get; set;
        } = null!;

    }
}
