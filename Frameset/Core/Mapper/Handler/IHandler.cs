using Frameset.Core.Mapper.Segment;
using System.Xml;

namespace Frameset.Core.Mapper.Handler
{
    public interface IHandler
    {

        AbstractSegment Parse(XmlElement element, string namespaceStr);
        //void DoProcessNode(XmlNode node,string namespaceStr,IList<AbstractSegment> segments);

    }
}
