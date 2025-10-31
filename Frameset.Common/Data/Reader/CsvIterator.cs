using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Spring.Globalization.Formatters;


namespace Frameset.Common.Data.Reader
{
    public class CsvIterator<T> : AbstractIterator<T>
    {
        public string Spliter
        {
            get; set;
        } = ",";
        internal long pos;
        internal DateTimeFormatter dateFormat;
        public CsvIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = true;
        }

        public CsvIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = true;
        }

        public override void BeforeProcess()
        {

        }
        public override bool MoveNext()
        {
            base.MoveNext();
            cachedValue.Clear();
            bool hasNext = false;
            if (reader != null)
            {
                string readStr = reader.ReadLine();
                pos++;
                if (!readStr.IsNullOrEmpty())
                {
                    string[] arr = readStr.Split(Spliter);
                    if (arr.Length >= MetaDefine.ColumnList.Count)
                    {
                        for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
                        {
                            DataSetColumnMeta meta = MetaDefine.ColumnList[i];
                            cachedValue.TryAdd(meta.ColumnCode, ConvertUtil.ConvertStringToTargetObject(arr[i], meta, dateFormat));
                            hasNext = true;
                        }
                    }
                    else
                    {
                        Log.Error(" line " + pos + " does't  have enough columns");
                    }
                }
            }
            if (hasNext)
            {
                ConstructReturn();
            }
            return hasNext;
        }

        public override void Dispose()
        {
            
        }
        public override IAsyncEnumerable<T> QueryAsync(IFileSystem fileSystem,string path = null)
        {

        }
    }
}
