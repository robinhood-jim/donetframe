using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;


namespace Frameset.Common.Data.Writer
{
    public class CsvDataWriter<T> : AbstractDataWriter<T>
    {
        private List<string> contents;
        private char sepearotr = ',';
        public CsvDataWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.CSV;
            useWriter = true;
            contents = new List<string>(define.ColumnList.Count);
            string separatorStr;
            define.ResourceConfig.TryGetValue("fs.outputSeparator", out separatorStr);
            if (!separatorStr.IsNullOrEmpty())
            {
                sepearotr = separatorStr[0];
            }
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
                    object retValue;
                    valueMap.TryGetValue(meta.ColumnCode, out retValue);
                    contents.Add(GetOutputString(meta, retValue));
                }
                else
                {
                    MethodParam param;
                    methodMap.TryGetValue(meta.ColumnCode, out param);
                    if (param != null)
                    {
                        object retValue = param.GetMethod.Invoke(value, null);
                        contents.Add(GetOutputString(meta, retValue));
                    }
                    else
                    {
                        contents.Add("");
                    }
                }
            }
            writer.WriteLine(String.Join(sepearotr, contents));
        }

    }
}
