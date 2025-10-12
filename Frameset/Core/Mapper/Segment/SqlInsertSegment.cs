using System.Collections.Generic;

namespace Frameset.Core.Mapper.Segment
{
    public class SqlInsertSegment : CompositeSegment
    {

        public bool UseGenerateKey
        {
            get; internal set;
        } = false;
        public string KeyProperty
        {
            get; internal set;
        }
        public string SequenceName
        {
            get; set;
        }
        public SqlInsertSegment(string nameSpace, string id, string type, string resultMap, string parameterType, IList<AbstractSegment> segments) : base(nameSpace, id, type, resultMap, parameterType, segments)
        {

        }
    }
}
