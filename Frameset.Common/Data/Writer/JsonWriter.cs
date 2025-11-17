using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;

namespace Frameset.Common.Data.Writer
{
    public class JsonWriter<T> : AbstractDataWriter<T>
    {
        public JsonWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.JSON;

        }

        public override void FinishWrite()
        {
            throw new NotImplementedException();
        }

        public override void WriteRecord(T value)
        {
            throw new NotImplementedException();
        }
    }
}
