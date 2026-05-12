using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;


namespace Frameset.Common.Data.Writer
{
    public class CsvDataWriter<T> : AbstractDataWriter<T>
    {
        private List<string> contents;
        private string sepearotr = ResourceConstants.CSVDEFAULTSPILTTER;
        bool containHeader = false;
        bool writeHeader = false;
        public CsvDataWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.CSV;
            useWriter = true;
            contents = new List<string>(define.ColumnList.Count);
            define.ResourceConfig.TryGetValue(ResourceConstants.CSVSPLITTER, out string? separatorStr);
            if (define.ResourceConfig.TryGetValue(ResourceConstants.CSVCONTAINHEADER, out string? containHeaderStr))
            {
                containHeader = Constants.VALID.Equals(containHeaderStr.Trim());
            }
            sepearotr = separatorStr ?? sepearotr;
            Initalize();
        }

        public CsvDataWriter(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.CSV;
            useWriter = true;
            contents = new List<string>(methodMap.Count);
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.CSVSPLITTER, out string? separatorStr);
            sepearotr = separatorStr ?? sepearotr;
            Initalize();
        }

        public override void FinishWrite()
        {
            Flush();
        }

        public override void WriteRecord(T value)
        {
            contents.Clear();
            if (!writeHeader && containHeader)
            {
                foreach (DataSetColumnMeta meta in MetaDefine.ColumnList)
                {
                    contents.Add(meta.ColumnName);
                }
                writer.WriteLine(string.Join(sepearotr[0], contents));
                contents.Clear();
                writeHeader = true;
            }
            foreach (DataSetColumnMeta meta in MetaDefine.ColumnList)
            {
                if (useDictOutput)
                {
                    Dictionary<string, object> valueMap = value as Dictionary<string, object>;
                    object retValue = null;
                    valueMap?.TryGetValue(meta.ColumnCode, out retValue);
                    contents.Add(GetOutputString(meta, retValue));
                }
                else
                {
                    methodMap.TryGetValue(meta.ColumnCode, out MethodParam? param);
                    if (param != null)
                    {
                        object retValue = param?.GetMethod.Invoke(value, null);
                        contents.Add(GetOutputString(meta, retValue));
                    }
                    else
                    {
                        contents.Add("");
                    }
                }
            }
            writer.WriteLine(string.Join(sepearotr[0], contents));
        }

    }
}
