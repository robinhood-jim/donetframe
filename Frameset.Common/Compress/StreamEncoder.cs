using Frameset.Core.Exceptions;
using Frameset.Core.Utils;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using K4os.Compression.LZ4.Streams;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors.Xz;
using System.Diagnostics;
using System.IO.Compression;

namespace Frameset.Common.Compress
{
    public static class StreamEncoder
    {
        public static Stream GetOutputByCompressType(string resourcePath, Stream rawstream)
        {
            Trace.Assert(rawstream != null);
            FileMeta meta = FileUtil.Parse(resourcePath, Path.DirectorySeparatorChar);
            Trace.Assert(meta != null);
            Stream outStream = meta.CompressCodec switch
            {
                CompressType.GZ => new GZipOutputStream(rawstream),
                CompressType.LZ4 => LZ4Stream.Encode(rawstream),
                CompressType.ZIP => GetZipOutput(rawstream, meta),
                CompressType.BZ2 => new BZip2OutputStream(rawstream),
                CompressType.ZSTD => new ZstdSharp.CompressionStream(rawstream),
                CompressType.BROTLI => new BrotliStream(rawstream, CompressionMode.Compress),
                CompressType.LZMA => new LzmaStream(LzmaEncoderProperties.Default, false, rawstream),
                CompressType.XZ => new XZStream(rawstream),
                CompressType.SNAPPY => new Snappier.SnappyStream(rawstream, CompressionMode.Compress),
                _ => rawstream
            };

            if (outStream != null)
            {
                return new BufferedStream(outStream);
            }
            else
            {
                throw new OperationFailedException("failed to get outputStream from " + resourcePath);
            }
        }
        private static Stream GetZipOutput(Stream rawstream, FileMeta meta)
        {
            Stream outStream = new ZipOutputStream(rawstream);
            ((ZipOutputStream)outStream).PutNextEntry(new ZipEntry(meta.FileName + "." + meta.FileFormat));
            return outStream;
        }
    }

}
