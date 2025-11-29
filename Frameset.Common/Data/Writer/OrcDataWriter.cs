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
        private OrcWriter orcWriter=null!;
        private Type dynamicType=null!;
        private Dictionary<string, PropertyInfo> propMap = [];


        public OrcDataWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.ORC;
            useRawOutputStream = true;
            Initalize();

        }

        public OrcDataWriter(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Trace.Assert(!IsReturnDictionary());
            Identifier = Constants.FileFormatType.ORC;
            useRawOutputStream = true;
            Initalize();
        }
        internal sealed override void Initalize()
        {
            base.Initalize();
            if (IsReturnDictionary())
            {
                string assignClassName = ResourceConstants.DYNAMICCLASSPREFIX + DateTime.Now.Second;
                if (MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.DYNAMICORCCLASSNAME, out string? className))
                {
                    if (!className.IsNullOrEmpty())
                    {
                        assignClassName = className;
                    }
                }
                dynamicType = DynamicClassCreator.CreateDynamicClass(assignClassName, MetaDefine.ColumnList);
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

        public override void FinishWrite()
        {
            if (orcWriter != null)
            {
                Flush();
                orcWriter.Dispose();
            }
        }

        public override void WriteRecord(T value)
        {
            if (useDictOutput)
            {
                dynamic? targetObject = Activator.CreateInstance(dynamicType);
                for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
                {
                    object? retVal = GetValue(value, MetaDefine.ColumnList[i]);
                    if (retVal != null)
                    {
                        PropertyInfo? info = null;
                        propMap.TryGetValue(MetaDefine.ColumnList[i].ColumnCode, out info);
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
                                ts = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(retVal?.ToString())).LocalDateTime;
                            }
                            
                            info?.SetMethod?.Invoke(targetObject, new object[] { ts });
                        }
                        else
                        {
                            info?.SetMethod?.Invoke(targetObject, new object[] { retVal });
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
