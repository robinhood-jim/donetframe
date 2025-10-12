using System.Collections.Generic;

namespace Frameset.Core.Mapper.Segment
{
    public class SqlSelectSegment : CompositeSegment
    {
        public SqlSelectSegment(string nameSpace, string id, string type, string resultMap, string parameterType, IList<AbstractSegment> segments) : base(nameSpace, id, type, resultMap, parameterType, segments)
        {

        }


    }
}
