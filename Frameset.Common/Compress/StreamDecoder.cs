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
    public class StreamDecoder
    {
        private StreamDecoder()
        {

        }
        public static Stream GetInputByCompressType(string resourcePath, Stream rawstream, long streamSize, char dirSep = '/')
        {
            Trace.Assert(rawstream != null);
            FileMeta meta = FileUtil.Parse(resourcePath, dirSep);
            Stream inputStream = meta.CompressCodec switch
            {
                CompressType.GZ => new GZipInputStream(rawstream),
                CompressType.LZ4 => LZ4Stream.Decode(rawstream),
                CompressType.ZIP => GetZipStream(rawstream),
                CompressType.BZ2 => new BZip2InputStream(rawstream),
                CompressType.ZSTD => new ZstdSharp.DecompressionStream(rawstream),
                CompressType.BROTLI => new BrotliStream(rawstream, CompressionMode.Decompress),
                CompressType.LZMA => GetLzmaStream(rawstream, streamSize),
                CompressType.XZ => new XZStream(rawstream),
                CompressType.SNAPPY => new Snappier.SnappyStream(rawstream, CompressionMode.Decompress),
                _ => rawstream
            };
            if (inputStream != null)
            {
                return new BufferedStream(inputStream);
            }
            else
            {
                throw new OperationFailedException("failed to read from " + resourcePath);
            }
        }
        private static Stream GetLzmaStream(Stream rawstream, long compressLength)
        {
            var lzmaEncodingStream = new LzmaStream(LzmaEncoderProperties.Default, false, rawstream);
            var properties = lzmaEncodingStream.Properties;
            lzmaEncodingStream.Close();
            return new LzmaStream(properties, rawstream, compressLength);
        }
        private static Stream GetZipStream(Stream rawstream)
        {
            ZipInputStream zipStream = new(rawstream);
            zipStream.GetNextEntry();
            return zipStream;
        }
    }
}
