using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Frameset.Core.Utils
{
    public class FileUtil
    {
        private static Dictionary<string, string> contentTypeMap = new Dictionary<string, string>();
        private static IList<string> suffix = new List<string>();
        private static IList<CompressType> compressTypes = new List<CompressType>();
        static FileUtil()
        {
            Type? type = MethodBase.GetCurrentMethod()?.DeclaringType;

            string? _namespace = type?.Namespace;

            Assembly _assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Frameset.Resources.contenttype.properties";

            Stream? stream = _assembly.GetManifestResourceStream(resourceName);
            string? line = null;
            Trace.Assert(stream != null);
            using (StreamReader reader = new StreamReader(stream))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    string[] arr = line.Split('=');
                    contentTypeMap.TryAdd(arr[0], arr[1]);
                }
            }

            foreach (CompressType compressType in Enum.GetValues(typeof(CompressType)))
            {
                suffix.Add(compressType.ToString().ToLower());
                compressTypes.Add(compressType);

            }
        }
        public static Stream? ReadStreamInPackage(string resourceName)
        {
            Assembly _assembly = Assembly.GetExecutingAssembly();
            return _assembly.GetManifestResourceStream(resourceName);
        }
        public static FileMeta Parse(string resourcePath, char? pathSepearatorStr = null)
        {
            char pathSeparator = pathSepearatorStr != null ? pathSepearatorStr.Value : Path.DirectorySeparatorChar;
            int pos = resourcePath.LastIndexOf(pathSeparator);
            if (pos == -1)
            {
                pathSeparator = '/';
                pos = resourcePath.LastIndexOf(pathSeparator);
            }
            FileMeta meta = new FileMeta();
            string fileName = string.Empty;
            string filePath;
            if (pos != -1)
            {
                if (pos < resourcePath.Length - 1)
                {
                    fileName = resourcePath.Substring(pos + 1);
                }
                filePath = resourcePath.Substring(0, pos);
            }
            else
            {
                filePath = "";
                fileName = resourcePath;
            }
            meta.Path = filePath;
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string[] namePart = fileName.Split(".");
                int suffixPos = 0;
                for (int i = namePart.Length - 1; i > 0; i--)
                {
                    if (CompressType.NONE.Equals(meta.CompressCodec))
                    {
                        int pos1 = suffix.IndexOf(namePart[i].ToLower());
                        if (pos1 != -1)
                        {
                            meta.CompressCodec = compressTypes[pos1];
                            continue;
                        }
                    }
                    contentTypeMap.TryGetValue(namePart[i].ToLower(), out string? contentType);
                    if (!string.IsNullOrWhiteSpace(contentType))
                    {
                        meta.FileFormat = namePart[i].ToLower();
                        meta.ContentType = contentType;
                        suffixPos = i;
                        break;
                    }
                }

                meta.FileName = string.Join('.', namePart.Take(suffixPos));
                meta.FullName = string.Join('.', namePart);
            }
            return meta;

        }
    }
    public class FileMeta
    {
        internal FileMeta()
        {

        }
        public string FullName
        {
            get; internal set;
        } = null!;
        public string FileName
        {
            get; internal set;
        } = null!;
        public string Path
        {
            get; internal set;
        } = null!;
        public CompressType CompressCodec
        {
            get; internal set;
        } = CompressType.NONE;
        public string FileFormat
        {
            get; internal set;
        } = null!;
        public string ContentType
        {
            get; internal set;
        } = null!;

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
