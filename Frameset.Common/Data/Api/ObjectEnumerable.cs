using Frameset.Common.Data.Reader;
using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.FileSystem;
using Frameset.Core.Utils;
using System.Collections;
using System.Diagnostics;

namespace Frameset.Common.Data.Api
{
    public class ObjectEnumerable<T> : IDisposable, IEnumerable<T>
    {
        private DataCollectionDefine define;
        private IFileSystem fileSystem;
        private FileMeta meta;
        private AbstractDataIterator<T> iterator;
        private bool isDisposed = false;
        public ObjectEnumerable(DataCollectionDefine define, string? processPath = null, char? pathSepearatorStr = null, Action<AbstractDataIterator<T>>? initFunc = null)
        {
            this.define = define;

            string processFile = processPath ?? define.Path;
            char pathSeparator = pathSepearatorStr != null ? pathSepearatorStr.Value : Path.DirectorySeparatorChar;
            meta = define.MetaData;
            if (meta == null)
            {
                meta = FileUtil.Parse(processFile, pathSeparator);
                define.MetaData = meta;
                define.FileFormat = Constants.FileFormatTypeOf(meta.FileFormat);
            }
            Trace.Assert(meta != null);
            define.Path = processFile;

            fileSystem = FileSystemFactory.GetFileSystem(define);
            if (meta == null)
            {
                throw new OperationFailedException("file path " + processFile + " parse failed");
            }
            iterator = DataFileImporter.GetEnumerator<T>(define, Constants.FileFormatTypeOf(meta.FileFormat), fileSystem, initFunc);
        }

        public void Dispose()
        {
            if (iterator != null)
            {
                iterator.Dispose();
            }
            isDisposed = true;
        }
        public void Stop()
        {
            if (iterator != null)
            {
                iterator.Stop();
            }
        }
        public bool IsDisposed()
        {
            return isDisposed;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return iterator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public AbstractDataIterator<T> GetReader()
        {
            return iterator;
        }
    }
}
