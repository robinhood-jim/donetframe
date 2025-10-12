using System.Collections.Generic;

namespace Frameset.Core.Mapper.Segment
{
    public class SqlConstantSegment : AbstractSegment
    {
        public SqlConstantSegment(string nameSpace, string id, string value) : base(nameSpace, id, value)
        {

        }

        public override string ReturnSqlPart(Dictionary<string, object> map)
        {
            return Value;
        }
    }
}
