using Frameset.Common.Data.Reader;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Utils;

namespace Frameset.Common.Data.Api
{
    public class DateFileImporter
    {
        public static async IAsyncEnumerator<T> QueryAsyn<T>(DataCollectionDefine collectionDefine,string? processPath = null)
        {
            string processFile = processPath?? collectionDefine.Path;
            FileMeta meta = FileUtil.Parse(processFile);
            IFileSystem fileSystem = FileSystemFactory.GetFileSystem(collectionDefine);
            AbstractIterator<T> iterator = null;
            if (meta != null)
            {
                switch (Constants.FileFormatTypeOf(meta.FileFormat))
                {
                    case Constants.FileFormatType.CSV:
                        iterator = new CsvIterator<T>(collectionDefine, fileSystem);
                        break;
                    case Constants.FileFormatType.XML:
                        iterator = new XmlIterator<T>(collectionDefine, fileSystem);
                        break;
                    case Constants.FileFormatType.JSON:
                        iterator = new JsonIterator<T>(collectionDefine, fileSystem);
                        break;

                }
            }
            
            await foreach(var item in iterator.QueryAsync(fileSystem, processFile).ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }
}
