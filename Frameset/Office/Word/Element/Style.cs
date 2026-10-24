namespace Frameset.Office.Word.Element
{
    public class Style
    {
        public string StyleId
        {
            get; set;
        } = string.Empty;
        public string TypeDef
        {
            get; set;
        } = string.Empty;
        public string DefaultVal
        {
            get; set;
        } = string.Empty;
        public string BaseOn
        {
            get; set;
        } = string.Empty;
        public string Name
        {
            get; set;
        } = string.Empty;
        public string QFormat
        {
            get; set;
        } = string.Empty;
        public string UiPriority
        {
            get; set;
        } = string.Empty;
        public PrDefaultRpr Rpr
        {
            get; set;
        } = null!;
    }
}
