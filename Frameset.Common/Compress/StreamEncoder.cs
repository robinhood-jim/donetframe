using Frameset.Core.Utils;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using K4os.Compression.LZ4.Streams;
using SharpCompress.Compressors.LZMA;
using SharpCompress.Compressors.Xz;
using System.IO.Compression;

namespace Frameset.Common.Compress
{
    public class StreamEncoder
    {
        internal StreamEncoder()
        {

        }
        public static Stream GetOutputByCompressType(string resourcePath, Stream rawstream)
        {
            Stream inputStream = null;
            FileMeta meta = FileUtil.Parse(resourcePath, Path.DirectorySeparatorChar);
            if (meta != null)
            {
                switch (meta.CompressCodec)
                {
                    case CompressType.GZ:
                        inputStream = new GZipOutputStream(rawstream);
                        break;
                    case CompressType.LZ4:
                        inputStream = LZ4Stream.Encode(rawstream);
                        break;
                    case CompressType.ZIP:
                        inputStream = new ZipOutputStream(rawstream);
                        break;
                    case CompressType.BZ2:
                        inputStream = new BZip2OutputStream(rawstream);
                        break;
                    case CompressType.ZSTD:
                        inputStream = new ZstdSharp.CompressionStream(rawstream);
                        break;
                    case CompressType.BROTLI:
                        inputStream = new BrotliStream(rawstream, CompressionMode.Compress);
                        break;
                    case CompressType.LZMA:
                        inputStream = new LzmaStream(LzmaEncoderProperties.Default, false, rawstream);
                        break;
                    case CompressType.XZ:
                        inputStream = new XZStream(rawstream);
                        break;
                    case CompressType.SNAPPY:
                        inputStream = new Snappier.SnappyStream(rawstream, CompressionMode.Compress);
                        break;
                    default:
                        inputStream = rawstream;
                        break;
                }
            }
            return inputStream;
        }
    }
}
