using System.Collections.Generic;

namespace Frameset.Core.Mapper.Segment
{
    public class SqlSegment : AbstractSegment
    {
        public SqlSegment(string nameSpace, string id, string value) : base(nameSpace, id, value)
        {

        }

        public override string ReturnSqlPart(Dictionary<string, object> map)
        {
            return Value;
        }
    }
}
