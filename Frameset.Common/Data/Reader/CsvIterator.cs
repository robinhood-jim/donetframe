using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using Serilog;


namespace Frameset.Common.Data.Reader
{
    public class CsvIterator<T> : AbstractDataIterator<T>
    {
        public string Spliter
        {
            get; set;
        } = ",";
        internal long pos;

        public CsvIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = true;
            initalize(define.Path);
        }

        public CsvIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = true;
            initalize(define.Path);
        }

        public override void initalize(string filePath = null)
        {
            base.initalize(filePath);
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
                hasNext = doProcess(readStr);
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
        private bool doProcess(string readStr)
        {
            bool hasNext = false;
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
                    ConstructReturn();
                }
                else
                {
                    Log.Error(" line " + pos + " does't  have enough columns");
                }
            }
            return hasNext;
        }


        public override async IAsyncEnumerable<T> ReadAsync(string path, string filterSql = null)
        {
            string line;
            if (!filterSql.IsNullOrEmpty())
            {

            }
            initalize(path);
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (doProcess(line))
                {
                    ConstructReturn();
                    yield return current;
                }
            }
        }
    }
}
