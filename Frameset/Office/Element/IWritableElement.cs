using Frameset.Office.Core;
namespace Frameset.Office.Element
{
    public interface IWritableElement
    {
        void WriteOut(XmlBufferWriter writer);

    }
    public enum SheetVisibility
    {
        VISIBLE,
        HIDDEN,
        VERY_HIDDEN
    }
}
