using System.Collections.Generic;

namespace Frameset.Core.Mapper.Segment
{
    public class SqlDeleteSegment : CompositeSegment
    {
        public SqlDeleteSegment(string nameSpace, string id, string type, string resultMap, string parameterType, IList<AbstractSegment> segments) : base(nameSpace, id, type, resultMap, parameterType, segments)
        {

        }
    }
}
