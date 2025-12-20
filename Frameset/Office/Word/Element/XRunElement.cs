using Frameset.Office.Meta;
using System.Collections.Generic;

namespace Frameset.Office.Word.Element
{
    public class XRunElement : IBodyElement
    {
        public List<PictureData> PictureDatas
        {
            get; internal set;
        } = [];

        public string Id
        {
            get; set;
        }
        public string Content
        {
            get; set;
        }
        public XRunElement(string id)
        {
            Id = id;
        }

        public WordEnumType.BodyType GetBodyType()
        {
            return WordEnumType.BodyType.RUN;
        }

        public RelationShip GetRelation()
        {
            return null;
        }

        WordEnumType.BodyElementType IBodyElement.GetType()
        {
            return WordEnumType.BodyElementType.RUN;
        }
    }
}
