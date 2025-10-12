using System.Collections.Generic;
using System.Text;

namespace Frameset.Core.Mapper.Segment
{
    public abstract class CompositeSegment : AbstractSegment
    {
        public string ResultMap
        {
            get; internal set;
        }
        public string CoType
        {
            get; internal set;
        }
        public string Parametertype
        {
            get; internal set;
        }
        public IList<AbstractSegment> Segments
        {
            get; internal set;
        }
        public CompositeSegment(string nameSpace, string id, string value) : base(nameSpace, id, value)
        {

        }
        public CompositeSegment(string nameSpace, string id, string type, string resultMap, string parameterType, IList<AbstractSegment> segments) : base(nameSpace, id, null)
        {
            this.CoType = type;
            this.ResultMap = resultMap;
            this.Parametertype = parameterType;
            this.Segments = segments;

        }

        public override string ReturnSqlPart(Dictionary<string, object> map)
        {
            StringBuilder builder = new StringBuilder();
            foreach (AbstractSegment segment in Segments)
            {
                builder.Append(segment.ReturnSqlPart(map));
            }
            return builder.ToString();
        }
    }
}
