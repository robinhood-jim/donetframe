using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;


namespace Frameset.Common.Data.Writer
{
    public class CsvWriter<T> : AbstractDataWriter<T>
    {
        private List<string> contents;
        private char sepearotr = ',';
        public CsvWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
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
            flush();
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
                    contents.Add(getOutput(meta, retValue));
                }
                else
                {
                    MethodParam param;
                    methodMap.TryGetValue(meta.ColumnCode, out param);
                    if (param != null)
                    {
                        object retValue = param.GetMethod.Invoke(value, null);
                        contents.Add(getOutput(meta, retValue));
                    }
                    else
                    {
                        contents.Add("");
                    }
                }
            }
            writer.WriteLine(String.Join(sepearotr, contents));
        }
        internal string getOutput(DataSetColumnMeta meta, object value)
        {
            if (value != null)
            {
                if (meta.ColumnType == Constants.MetaType.TIMESTAMP || meta.ColumnType == Constants.MetaType.DATE)
                {
                    if (value.GetType().Equals(typeof(DateTime)))
                    {
                        return dateFormat.Format((DateTime)value);
                    }
                    else
                    {
                        return value.ToString();
                    }
                }
                else
                {
                    return value.ToString();
                }
            }
            else
            {
                return "";
            }
        }
    }
}
