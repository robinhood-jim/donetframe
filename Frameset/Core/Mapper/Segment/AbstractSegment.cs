using System.Collections.Generic;

namespace Frameset.Core.Mapper.Segment
{
    public abstract class AbstractSegment
    {
        public string Value
        {
            get; set;
        } = null!;
        public string Namespace
        {
            get; set;
        } = null!;
        public string Id
        {
            get; set;
        } = null!;
        protected AbstractSegment(string nameSpace, string id, string value)
        {
            this.Id = id;
            this.Namespace = nameSpace;
            this.Value = value;
        }
        public abstract string ReturnSqlPart(Dictionary<string, object> map);

    }

}
