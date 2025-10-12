using System.Collections.Generic;

namespace Frameset.Core.Mapper.Segment
{
    public class SqlBatchSegement : CompositeSegment
    {
        public SqlBatchSegement(string nameSpace, string id, string type, string resultMap, string parameterType, IList<AbstractSegment> segments) : base(nameSpace, id, type, resultMap, parameterType, segments)
        {

        }
    }
}
