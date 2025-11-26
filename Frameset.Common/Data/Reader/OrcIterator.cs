using ApacheOrcDotNet;
using Frameset.Common.FileSystem;
using Frameset.Common.Util;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;

namespace Frameset.Common.Data.Reader
{
    public class OrcIterator<T> : AbstractDataIterator<T>
    {
        private OrcReader orcreader;
        private Type dynamicType;
        private Dictionary<string, PropertyInfo> propMap = new Dictionary<string, PropertyInfo>();
        private IEnumerator<object> values;
        public OrcIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            Initalize(define.Path);
        }
        public OrcIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            Initalize(define.Path);
        }

        public OrcIterator(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.AVRO;
            useRawStream = true;
            Initalize(processPath);
        }

        public override void Initalize(string filePath = null)
        {
            base.Initalize(filePath);
            string className;
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

            orcreader = new OrcReader(dynamicType, inputStream);
            values = orcreader.Read().GetEnumerator();
        }

        public override IAsyncEnumerable<T> ReadAsync(string path = null, string filterSql = null)
        {
            throw new NotImplementedException();
        }
        public override bool MoveNext()
        {
            base.MoveNext();
            bool hasNext = values.MoveNext();
            if (hasNext)
            {
                cachedValue.Clear();
                object obj = values.Current;
                foreach (DataSetColumnMeta column in MetaDefine.ColumnList)
                {
                    object value = propMap[column.ColumnCode].GetGetMethod().Invoke(obj, null);
                    if (value != null)
                    {
                        cachedValue.TryAdd(column.ColumnCode, value);
                    }
                }
                ConstructReturn();
            }
            return hasNext;
        }
    }
}
