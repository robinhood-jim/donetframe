using System.Collections.Generic;

namespace Frameset.Core.Mapper.Segment
{
    public class IncludeSegment : AbstractSegment
    {
        public IncludeSegment(string nameSpace, string id, string value) : base(nameSpace, id, value)
        {

        }

        public override string ReturnSqlPart(Dictionary<string, object> map)
        {
            return SqlMapperConfigure.GetSqlPart(Namespace, Value);
        }
    }
}
