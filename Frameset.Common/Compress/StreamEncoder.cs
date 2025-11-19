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
            Stream outStream = null;
            FileMeta meta = FileUtil.Parse(resourcePath, Path.DirectorySeparatorChar);
            if (meta != null)
            {
                switch (meta.CompressCodec)
                {
                    case CompressType.GZ:
                        outStream = new GZipOutputStream(rawstream);
                        break;
                    case CompressType.LZ4:
                        outStream = LZ4Stream.Encode(rawstream);
                        break;
                    case CompressType.ZIP:
                        outStream = new ZipOutputStream(rawstream);
                        ((ZipOutputStream)outStream).PutNextEntry(new ZipEntry(meta.FileName + "." + meta.FileFormat));
                        break;
                    case CompressType.BZ2:
                        outStream = new BZip2OutputStream(rawstream);
                        break;
                    case CompressType.ZSTD:
                        outStream = new ZstdSharp.CompressionStream(rawstream);
                        break;
                    case CompressType.BROTLI:
                        outStream = new BrotliStream(rawstream, CompressionMode.Compress);
                        break;
                    case CompressType.LZMA:
                        outStream = new LzmaStream(LzmaEncoderProperties.Default, false, rawstream);
                        break;
                    case CompressType.XZ:
                        outStream = new XZStream(rawstream);
                        break;
                    case CompressType.SNAPPY:
                        outStream = new Snappier.SnappyStream(rawstream, CompressionMode.Compress);
                        break;
                    default:
                        outStream = rawstream;
                        break;
                }
            }
            return outStream;
        }
    }
}
