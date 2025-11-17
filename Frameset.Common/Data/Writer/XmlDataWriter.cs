using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;

namespace Frameset.Common.Data.Writer
{
    public class XmlDataWriter<T> : AbstractDataWriter<T>
    {
        public XmlDataWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
            Identifier = Constants.FileFormatType.XML;

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
