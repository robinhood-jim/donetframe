using Frameset.Web.Model.System;

namespace Frameset.Web.Dto
{
    public class SysCodeDto
    {
        public string Label
        {
            get; set;
        } = string.Empty;
        public string Value
        {
            get; set;
        } = string.Empty;
        public string ParentCodeId
        {
            get; set;
        } = string.Empty;
        public string ParentCodeValue
        {
            get; set;
        } = string.Empty;
        public SysCodeDto(string codeName, string value)
        {
            Label = codeName;
            Value = value;
        }
        public SysCodeDto(string codeName, string value, string pcodeId, string pvalue) : this(codeName, value)
        {
            ParentCodeId = pcodeId;
            ParentCodeValue = pvalue;
        }
        public static SysCodeDto FromModel(SysCode code)
        {
            return new(code.Name, code.Value);
        }


    }
}
