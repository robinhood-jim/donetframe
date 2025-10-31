using Frameset.Common.FileSystem;
using Frameset.Core.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frameset.Common.Data.Reader
{
    public class AvroIterator<T> : AbstractIterator<T>
    {
        public AvroIterator(DataCollectionDefine define) : base(define)
        {
        }

        public AvroIterator(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {
        }

        public override IAsyncEnumerable<T> QueryAsync(IFileSystem fileSystem, string path = null)
        {
            throw new NotImplementedException();
        }
    }
}
