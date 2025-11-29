using Frameset.Common.FileSystem;
using Frameset.Core.FileSystem;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Frameset.Common.Data.Writer
{
    public class ProtoBufWriter<T> : AbstractDataWriter<T>
    {
        private MessageDescriptor descriptor=null!;
        MessageParser parser=null!;

        public ProtoBufWriter(DataCollectionDefine define, IFileSystem fileSystem) : base(define, fileSystem)
        {

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
