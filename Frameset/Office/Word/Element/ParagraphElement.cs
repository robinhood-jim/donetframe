using Frameset.Office.Meta;
using System.Collections.Generic;

namespace Frameset.Office.Word.Element
{
    public class ParagraphElement : IBodyElement
    {
        public string Id
        {
            get; internal set;
        }
        public string RsidR
        {
            get; internal set;
        }
        public string RsidRDefault
        {
            get; internal set;
        }
        public XRunElement Element
        {
            get; set;
        }
        public List<XRunElement> Elements
        {
            get; internal set;
        } = [];
        public string RunId
        {
            get; set;
        }
        public ParagraphElement(string id, string rsidR, string rsidRDefault, string runId, List<XRunElement> elements)
        {
            this.Id = id;
            RsidR = rsidR;
            RsidRDefault = rsidRDefault;
            RunId = runId;
            Elements = elements;
        }


        public WordEnumType.BodyType GetBodyType()
        {
            return WordEnumType.BodyType.PARAGRAPH;
        }

        public RelationShip GetRelation()
        {
            return null;
        }

        WordEnumType.BodyElementType IBodyElement.GetType()
        {
            return WordEnumType.BodyElementType.PARAGRAPH;
        }
    }
}
