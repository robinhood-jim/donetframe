using Frameset.Office.Meta;

namespace Frameset.Office.Word.Element
{
    public interface IBodyElement
    {
        WordEnumType.BodyType GetBodyType();
        RelationShip GetRelation();
        WordEnumType.BodyElementType GetType();
    }
}
