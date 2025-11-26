using Frameset.Core.Mapper.Handler;
using Frameset.Core.Mapper.Segment;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Frameset.Core.Mapper
{
    public class SqlMapperConfigure
    {
        public static readonly string SELECT = "select";
        public static readonly string RESULTMAP = "resultMap";
        public static readonly string INSERT = "insert";
        public static readonly string UPDATE = "update";
        public static readonly string BATCH = "batch";
        public static readonly string INCLUDE = "include";
        public static readonly string SCRIPT = "script";
        public static readonly string SQL = "sql";
        private static Dictionary<string, MapperConfig> configMap = new Dictionary<string, MapperConfig>();
        private static Dictionary<string, Dictionary<string, AbstractSegment>> executeMap = new Dictionary<string, Dictionary<string, AbstractSegment>>();

        private static IList<string> operType = new string[] { "select", "update", "delete", "insert" }.ToList();
        protected SqlMapperConfigure()
        {

        }
        public static void DoInit(string mapperPath)
        {
            FileInfo[] infos = ConfigResourceScanner.DoScan(mapperPath);
            if (!infos.IsNullOrEmpty())
            {
                foreach (FileInfo info in infos)
                {
                    LoadConfig(info.FullName);
                }
            }
        }
        internal static void LoadConfig(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var doc = new XmlDocument();

                using (XmlReader xmlReader = XmlReader.Create(stream))
                {
                    doc.Load(xmlReader);
                    XmlNode pnode = doc.SelectSingleNode("mapper");
                    string namespaceStr = ((XmlElement)pnode).GetAttribute("namespace");
                    configMap.TryAdd(namespaceStr, new MapperConfig());
                    executeMap.TryAdd(namespaceStr, new Dictionary<string, AbstractSegment>());
                    XmlNodeList nodeList = pnode.ChildNodes;
                    foreach (XmlNode node in nodeList)
                    {
                        XmlElement ele = (XmlElement)node;
                        string id = ele.GetAttribute("id");
                        string type = node.Name;
                        if (operType.Contains(type))
                        {
                            AbstractSegment segment = CompositeHandler.Parse(ele, namespaceStr);

                            executeMap[namespaceStr].TryAdd(id, segment);
                        }
                        else
                        {
                            switch (type)
                            {
                                case "sql":
                                    configMap[namespaceStr].SqlMap.TryAdd(id, ele.InnerText);
                                    break;
                                case "resultMap":
                                    string resultType = ele.GetAttribute("type");
                                    ResultMap rsMap = new ResultMap(resultType);

                                    foreach (XmlElement ele1 in ele.ChildNodes)
                                    {
                                        if (ele1.GetAttribute("property") != null && ele1.GetAttribute("column") != null)
                                        {
                                            rsMap.MappingColumns.TryAdd(ele1.GetAttribute("column"), ele1.GetAttribute("property"));
                                        }

                                    }
                                    configMap[namespaceStr].ReslutMap.TryAdd(id, rsMap);
                                    break;

                            }

                        }
                    }
                }



            }

        }
        public static AbstractSegment GetExecuteSegment(string nameSpace, string id)
        {
            AbstractSegment segment = null;
            Dictionary<string, AbstractSegment> segmentMap = null;
            if (executeMap.TryGetValue(nameSpace, out segmentMap))
            {
                segmentMap.TryGetValue(id, out segment);
            }
            return segment;
        }
        public static ResultMap GetResultMap(string nameSpace, string id)
        {
            MapperConfig config = null;
            ResultMap map = null;
            if (configMap.TryGetValue(nameSpace, out config))
            {
                config.ReslutMap.TryGetValue(id, out map);
            }
            return map;
        }
        public static string GetSqlPart(string nameSpace, string id)
        {
            MapperConfig config = null;
            string ret = null;
            if (configMap.TryGetValue(nameSpace, out config))
            {
                config.SqlMap.TryGetValue(id, out ret);
            }
            return ret;
        }

    }
}
