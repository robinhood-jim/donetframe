using Frameset.Common.FileSystem;
using Frameset.Core.FileSystem;

namespace Frameset.Common.Data.Reader
{
    public class JsonIterator<T> : AbstractIterator<T>
    {
        public JsonIterator(DataCollectionDefine define) : base(define)
        {
        }

        public JsonIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
        }

        public override IAsyncEnumerable<T> QueryAsync(IFileSystem fileSystem, string path = null)
        {
            throw new NotImplementedException();
        }
    }
}
