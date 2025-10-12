using System.Collections.Generic;

namespace Frameset.Core.Mapper.Segment
{
    public class SqlUpdateSegment : CompositeSegment
    {
        public SqlUpdateSegment(string nameSpace, string id, string type, string resultMap, string parameterType, IList<AbstractSegment> segments) : base(nameSpace, id, type, resultMap, parameterType, segments)
        {

        }
    }
}
