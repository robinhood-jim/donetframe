using Frameset.Core.Mapper.Segment;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Xml;

namespace Frameset.Core.Mapper.Handler
{

    public abstract class CompositeHandler
    {

        public static AbstractSegment Parse(XmlElement element, string namespaceStr)
        {
            string type = element.Name;
            string id = element.GetAttribute("id");
            IList<AbstractSegment> segments = new List<AbstractSegment>();
            if (element.HasChildNodes)
            {
                foreach (XmlNode cele in element.ChildNodes)
                {
                    if (cele.GetType().Equals(typeof(XmlText)))
                    {
                        segments.Add(new SqlConstantSegment(namespaceStr, "", cele.InnerText));
                    }
                    else
                    {
                        ParseElement((XmlElement)cele, namespaceStr, segments);
                    }
                }
            }
            else
            {
                segments.Add(new SqlConstantSegment(namespaceStr, "", element.InnerText));
            }
            CompositeSegment segment = null;
            switch (type)
            {
                case "select":
                    segment = new SqlSelectSegment(namespaceStr, id, element.GetAttribute("type"), element.GetAttribute("resultMap"), element.GetAttribute("parameterType"), segments);
                    break;
                case "update":
                    segment = new SqlUpdateSegment(namespaceStr, id, element.GetAttribute("type"), element.GetAttribute("resultMap"), element.GetAttribute("parameterType"), segments);
                    break;
                case "insert":
                    segment = new SqlInsertSegment(namespaceStr, id, element.GetAttribute("type"), element.GetAttribute("resultMap"), element.GetAttribute("parameterType"), segments);
                    if ("true".Equals(element.GetAttribute("useGeneratedKeys")))
                    {
                        ((SqlInsertSegment)segment).UseGenerateKey = true;
                        ((SqlInsertSegment)segment).KeyProperty = element.GetAttribute("keyProperty");
                    }
                    if (!element.GetAttribute("sequenceName").IsNullOrEmpty())
                    {
                        ((SqlInsertSegment)segment).SequenceName = element.GetAttribute("sequenceName");
                    }
                    break;
                case "delete":
                    segment = new SqlDeleteSegment(namespaceStr, id, element.GetAttribute("type"), element.GetAttribute("resultMap"), element.GetAttribute("parameterType"), segments);
                    break;
                case "batch":
                    segment = new SqlBatchSegement(namespaceStr, id, element.GetAttribute("type"), element.GetAttribute("resultMap"), element.GetAttribute("parameterType"), segments);
                    break;
            }
            return segment;
        }

        public static void ParseElement(XmlElement element, string nameSpace, IList<AbstractSegment> segments)
        {
            string elType = element.Name;
            string id = element.GetAttribute("id");
            if (element.ChildNodes.Count == 1 && elType == null)
            {
                segments.Add(new SqlConstantSegment(nameSpace, "", element.InnerText));
            }
            else
            {
                switch (elType)
                {
                    case "include":
                        string refId = element.GetAttribute("refid");
                        segments.Add(new IncludeSegment(nameSpace, id, refId));
                        break;
                    case "script":
                        string language = element.GetAttribute("lang");
                        string scriptContent = element.InnerText;
                        ScriptSegment segment = new ScriptSegment(nameSpace, id, scriptContent);
                        segment.ScriptType = language;
                        segment.Init();
                        segments.Add(segment);
                        break;
                }
            }
        }
    }
}
