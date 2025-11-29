using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using Serilog;


namespace Frameset.Common.Data.Reader
{
    public class CsvIterator<T> : AbstractDataIterator<T>
    {
        public string Splitter
        {
            get; set;
        } = ResourceConstants.CSVDEFAULTSPILTTER;
        internal long pos;

        public CsvIterator(DataCollectionDefine define) : base(define)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = true;
            Initalize(define.Path);
        }

        public CsvIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = true;
            Initalize(define.Path);
        }

        public CsvIterator(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.CSV;
            useReader = true;
            Initalize(processPath);
        }

        public override sealed void Initalize(string? filePath = null)
        {
            base.Initalize(filePath);
            string? splitterStr;
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.CSVSPLITTER, out splitterStr);
            if (!splitterStr.IsNullOrEmpty())
            {
                Splitter = splitterStr;
            }
        }
        public override bool MoveNext()
        {
            base.MoveNext();
            CachedValue.Clear();
            bool hasNext = false;
            try
            {
                if (reader != null)
                {
                    string? readStr = reader?.ReadLine();
                    pos++;
                    hasNext = doProcess(readStr);
                }
                if (hasNext)
                {
                    ConstructReturn();
                }
            }
            catch (EndOfStreamException ex)
            {
                Log.Error("end of stream {Message}", ex.Message);
            }
            return hasNext;
        }
        private bool doProcess(string? readStr)
        {
            bool hasNext = false;
            if (!readStr.IsNullOrEmpty())
            {
                string[] arr = readStr?.Split(Splitter);
                if (arr?.Length >= MetaDefine.ColumnList.Count)
                {
                    for (int i = 0; i < MetaDefine.ColumnList.Count; i++)
                    {
                        DataSetColumnMeta meta = MetaDefine.ColumnList[i];
                        if (meta.ColumnType != Constants.MetaType.TIMESTAMP)
                        {
                            CachedValue.TryAdd(meta.ColumnCode, ConvertUtil.ConvertStringToTargetObject(arr[i], meta, dateFormatter));
                        }
                        else
                        {
                            CachedValue.TryAdd(meta.ColumnCode, ConvertUtil.ConvertStringToTargetObject(arr[i], meta, timestampFormatter));
                        }
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


        public override async IAsyncEnumerable<T> ReadAsync(string path, string? filterSql = null)
        {
            string? line;
            if (!filterSql.IsNullOrEmpty())
            {

            }
            Initalize(path);
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
