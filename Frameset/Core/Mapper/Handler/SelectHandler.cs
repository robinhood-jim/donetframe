using Frameset.Core.Mapper.Segment;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Frameset.Core.Mapper.Handler
{
    public class SelectHandler : CompositeHandler
    {
        public void DoProcessNode(XmlNode node, string namespaceStr, IList<AbstractSegment> segments)
        {
            if (node.InnerText != null)
            {
                segments.Add(new SqlConstantSegment(namespaceStr, null, node.InnerText));
            }
            else
            {
                Parse((XmlElement)node, namespaceStr);
            }
            throw new NotImplementedException();
        }


    }
}
