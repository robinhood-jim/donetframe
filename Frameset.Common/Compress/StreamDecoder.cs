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
        internal StreamDecoder()
        {

        }
        public static Stream? GetInputByCompressType(string resourcePath, Stream rawstream, char dirSep = '/')
        {
            Trace.Assert(rawstream != null);
            Stream inputStream = null;
            FileMeta meta = FileUtil.Parse(resourcePath, dirSep);
            if (meta != null)
            {
                switch (meta.CompressCodec)
                {
                    case CompressType.GZ:
                        inputStream = new GZipInputStream(rawstream);
                        break;
                    case CompressType.LZ4:
                        inputStream = LZ4Stream.Decode(rawstream);
                        break;
                    case CompressType.ZIP:
                        inputStream = new ZipInputStream(rawstream);
                        break;
                    case CompressType.BZ2:
                        inputStream = new BZip2InputStream(rawstream);
                        break;
                    case CompressType.ZSTD:
                        inputStream = new ZstdSharp.DecompressionStream(rawstream);
                        break;
                    case CompressType.BROTLI:
                        inputStream = new BrotliStream(rawstream, CompressionMode.Decompress);
                        break;
                    case CompressType.LZMA:
                        inputStream = new LzmaStream(LzmaEncoderProperties.Default, false, rawstream);
                        break;
                    case CompressType.XZ:
                        inputStream = new XZStream(rawstream);
                        break;
                    case CompressType.SNAPPY:
                        inputStream = new Snappier.SnappyStream(rawstream, CompressionMode.Decompress);
                        break;
                    default:
                        inputStream = rawstream;
                        break;
                }
            }
            if (inputStream != null)
            {
                return new BufferedStream(inputStream);
            }
            else
            {
                throw new OperationFailedException("failed to read from " + resourcePath);
            }
        }
    }
}
