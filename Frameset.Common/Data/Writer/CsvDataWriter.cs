using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;


namespace Frameset.Common.Data.Writer
{
    public class CsvDataWriter<T> : AbstractDataWriter<T>
    {
        private List<string> contents;
        private char sepearotr = ResourceConstants.CSVDEFAULTSPILTTER[0];
        public CsvDataWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.CSV;
            useWriter = true;
            contents = new List<string>(define.ColumnList.Count);
            define.ResourceConfig.TryGetValue(ResourceConstants.CSVSPLITTER, out string? separatorStr);
            if (!separatorStr.IsNullOrEmpty())
            {
                sepearotr = separatorStr[0];
            }
            Initalize();
        }

        public CsvDataWriter(IFileSystem fileSystem, string processPath) : base(fileSystem, processPath)
        {
            Identifier = Constants.FileFormatType.CSV;
            useWriter = true;
            contents = new List<string>(methodMap.Count);
            MetaDefine.ResourceConfig.TryGetValue(ResourceConstants.CSVSPLITTER, out string? separatorStr);
            if (!separatorStr.IsNullOrEmpty())
            {
                sepearotr = separatorStr[0];
            }
            Initalize();
        }

        public override void FinishWrite()
        {
            Flush();
        }

        public override void WriteRecord(T value)
        {
            contents.Clear();
            foreach (DataSetColumnMeta meta in MetaDefine.ColumnList)
            {
                if (useDictOutput)
                {
                    Dictionary<string, object> valueMap = value as Dictionary<string, object>;
                    object? retValue = null;
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
            writer.WriteLine(string.Join(sepearotr, contents));
        }

    }
}
