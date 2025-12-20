using Frameset.Office.Meta;
using System.Collections.Generic;

namespace Frameset.Office.Word.Element
{
    public class TableElement : IBodyElement
    {
        public List<string> Headers
        {
            get; private set;
        }
        public List<List<string>> Values
        {
            get; private set;
        }
        public TableElement(List<string> headers, List<List<string>> values)
        {
            this.Headers = headers;
            this.Values = values;
        }

        public WordEnumType.BodyType GetBodyType()
        {
            return WordEnumType.BodyType.TABLECELL;
        }

        public RelationShip GetRelation()
        {
            return null;
        }

        WordEnumType.BodyElementType IBodyElement.GetType()
        {
            return WordEnumType.BodyElementType.TABLE;
        }
    }
}
