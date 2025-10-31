using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Frameset.Core.Utils
{
    public class FileUtil
    {
        private static Dictionary<string, string> contentTypeMap;
        private static IList<string> suffix = new List<string>();
        private static IList<CompressType> compressTypes = new List<CompressType>();
        static FileUtil()
        {
            string contentTypeStr = AppConfigurtaionServices.Configuration["SysParam.ContentTypes"];
            contentTypeMap = (Dictionary<string, string>)JsonSerializer.Deserialize(contentTypeStr, typeof(Dictionary<string, string>));
            foreach (CompressType type in Enum.GetValues(typeof(CompressType)))
            {
                suffix.Add(type.ToString().ToLower());
                compressTypes.Add(type);

            }

        }
        public static FileMeta Parse(string resourcePath, char separator = '\\')
        {
            char fileSeparator = separator;
            int pos = resourcePath.LastIndexOf(fileSeparator);
            if (pos == -1)
            {
                fileSeparator = Path.DirectorySeparatorChar;
                pos = resourcePath.LastIndexOf(fileSeparator);
            }
            if (pos != -1)
            {
                FileMeta meta = new FileMeta();
                string fileName = resourcePath.Substring(pos + 1);
                string filePath = resourcePath.Substring(0, pos);
                meta.Path = filePath;
                meta.FileName = fileName;
                string[] namePart = fileName.Split("\\.");
                for (int i = namePart.Length - 1; i > 0; i--)
                {
                    if (CompressType.NONE.Equals(meta.CompressCodec))
                    {
                        int pos1 = suffix.IndexOf(namePart[i].ToLower());
                        if (pos1 != -1)
                        {
                            meta.CompressCodec = compressTypes[pos1];
                        }
                    }
                    string contentType;
                    contentTypeMap.TryGetValue(namePart[i].ToLower(), out contentType);
                    if (!contentType.IsNullOrEmpty())
                    {
                        meta.FileFormat = namePart[i].ToLower();
                        meta.ContentType = contentType;
                        break;
                    }
                }
                return meta;

            }
            else
            {
                throw new ConfigMissingException("resourcePath is illegal");
            }

        }
    }
    public class FileMeta
    {
        internal FileMeta()
        {

        }
        public string FileName
        {
            get; internal set;
        }
        public string Path
        {
            get; internal set;
        }
        public CompressType CompressCodec
        {
            get; internal set;
        } = CompressType.NONE;
        public string FileFormat
        {
            get; internal set;
        }
        public string ContentType
        {
            get; internal set;
        }

    }
    public enum CompressType
    {
        NONE,
        GZ,
        ZIP,
        LZO,
        LZ4,
        BZ2,
        SNAPPY,
        BROTLI,
        LZMA,
        XZ,
        ZSTD,
        RAR
    }
}
