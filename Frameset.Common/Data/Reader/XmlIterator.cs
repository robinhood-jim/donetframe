using Frameset.Common.FileSystem;
using Frameset.Core.FileSystem;

namespace Frameset.Common.Data.Reader
{
    public class XmlIterator<T> : AbstractIterator<T>
    {
        public XmlIterator(DataCollectionDefine define) : base(define)
        {
        }

        public XmlIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
        }

        public override IAsyncEnumerable<T> QueryAsync(IFileSystem fileSystem, string path = null)
        {
            throw new NotImplementedException();
        }
    }
}
