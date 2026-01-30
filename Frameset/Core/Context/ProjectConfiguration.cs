using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Frameset.Core.Context
{
    public static class ProjectConfiguration
    {
        private static Dictionary<string, object> keyValues = [];
        public static void DoInit(string configFile)
        {
            try
            {
                IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
                StreamReader reader = File.OpenText(configFile);
                keyValues = deserializer.Deserialize<Dictionary<string, object>>(reader);
            }
            catch (FileNotFoundException ex)
            {
                Log.Error("config file not found!");
            }
            catch (Exception ex)
            {
                Log.Error("{Message}", ex.Message);
            }
        }
        public static T GetConfig<T>(string key)
        {
            keyValues.TryGetValue(key, out object retConfig);
            if (retConfig != null)
            {
                Trace.Assert(typeof(T) == retConfig.GetType(), "");
                return (T)retConfig;
            }
            return default;
        }
    }
}
