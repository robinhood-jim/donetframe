using ApacheOrcDotNet;
using Frameset.Common.FileSystem;
using Frameset.Common.Util;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Reflection;

namespace Frameset.Common.Data.Writer
{
    public class OrcDataWriter<T> : AbstractDataWriter<T>
    {
        private OrcWriter orcWriter;
        private Type dynamicType;
        private Dictionary<string, PropertyInfo> propMap = new Dictionary<string, PropertyInfo>();


        public OrcDataWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.ORC;
            initalize();
            string className;
            if (IsReturnDictionary())
            {
                MetaDefine.ResourceConfig.TryGetValue("orc.dynamicClassName", out className);
                if (className.IsNullOrEmpty())
                {
                    className = "DynamicObject" + DateTime.Now.Second;
                }
                dynamicType = DynamicClassCreator.CreateDynamicClass(className, MetaDefine.ColumnList);
                PropertyInfo[] infos = dynamicType.GetProperties();
                foreach (PropertyInfo info in infos)
                {
                    propMap.TryAdd(info.Name, info);
                }
                WriterConfiguration configuration = new WriterConfiguration();
            }
            else
            {
                dynamicType = typeof(T);
            }
            orcWriter = new OrcWriter(dynamicType, outputStream, new WriterConfiguration());
        }

        public OrcDataWriter(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Trace.Assert(!IsReturnDictionary());
            Identifier = Constants.FileFormatType.ORC;
            initalize();
            dynamicType = typeof(T);
            orcWriter = new OrcWriter(dynamicType, outputStream, new WriterConfiguration());
        }

        public override void FinishWrite()
        {
            if (orcWriter != null)
            {
                orcWriter.Dispose();
            }
        }

        public override void WriteRecord(T value)
        {
            if (useDictOutput)
            {
                dynamic targetObject = System.Activator.CreateInstance(dynamicType);
                for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
                {
                    object retVal = GetValue(value, MetaDefine.ColumnList[i]);
                    if (retVal != null)
                    {
                        if (MetaDefine.ColumnList[i].ColumnType == Constants.MetaType.TIMESTAMP)
                        {
                            object ts;
                            if (retVal is DateTime)
                            {
                                ts = retVal;
                            }
                            else if (retVal is DateTimeOffset)
                            {
                                ts = retVal;
                            }
                            else
                            {
                                ts = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(retVal.ToString())).LocalDateTime;
                            }
                            propMap[MetaDefine.ColumnList[i].ColumnCode].SetMethod.Invoke(targetObject, new object[] { ts });
                        }
                        else
                        {
                            propMap[MetaDefine.ColumnList[i].ColumnCode].SetMethod.Invoke(targetObject, new object[] { retVal });
                        }
                    }
                }
                orcWriter.AddRow(targetObject);
            }
            else
            {
                orcWriter.AddRow(value);
            }
        }
    }
}
