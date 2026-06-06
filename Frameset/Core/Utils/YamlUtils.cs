using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Frameset.Core.Utils
{
    public class YamlUtils
    {
        private static IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
        public static T Deserializer<T>(string inputContent)
        {
            return deserializer.Deserialize<T>(inputContent);
        }
        public static T Deserializer<T>(StreamReader reader)
        {
            return deserializer.Deserialize<T>(reader);
        }
    }
}
