using Frameset.Core.Common;
using Frameset.Core.Dao;
using Frameset.Core.Hardware;
using Frameset.Core.Mapper;
using Frameset.Core.Raft;
using Frameset.Core.Reflect;
using Frameset.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Frameset.Core.Configuration
{
    public class ProjectConfiguration : IDisposable
    {
        private Dictionary<string, object> configDict = [];
        private string etcdHostUrl;
        private EtcdServiceClient client = null!;
        private string appName = DEFAULT;
        private string mapperPath = null!;
        private string etcdWorkerBasePath = "/service/";
        private string etcdmasterPath = "/service/master";
        private string realIp;
        private bool masterAck = false;
        private TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        private Dictionary<string, Dictionary<string, string>> fileSystemConfig = [];
        private bool useMultipleFileSystem = false;
        public static readonly string DATASOURCE = "dataSource";
        public static readonly string SERVERHOST = "etcdHost";
        public static readonly string APPNAME = "appName";
        //启用类mybatisplus配置开关
        public static readonly string MAPPERPATH = "mapper";
        //多文件系统启用开关
        public static readonly string USERMULTIPLEFS = "useMFS";
        public static readonly string FILESYSTEMS = "fileSystem";
        public static readonly string DEFAULT = "default";
        public static readonly byte[] ALIVE = { 0x00, 0x00, 0x7F, 0x7F };
        private CancellationTokenSource cancellation;

        public ProjectConfiguration(string yamlConfigFile)
        {
            string yamlPath = yamlConfigFile;

            IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
            if (string.IsNullOrWhiteSpace(yamlPath))
            {
                yamlPath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "application.yml";
            }
            else if (string.Equals(yamlPath.Take(10).ToString(), "classpath:", StringComparison.OrdinalIgnoreCase))
            {
                yamlPath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + yamlPath.Skip(10).ToString();
            }
            using StreamReader reader = File.OpenText(yamlPath);
            configDict = deserializer.Deserialize<Dictionary<string, object>>(reader);
            object dataSourceDict = null;
            configDict.TryGetValue(DATASOURCE, out dataSourceDict);
            configDict.TryGetValue(SERVERHOST, out object serverHostObj);
            configDict.TryGetValue(APPNAME, out object appNameObj);
            configDict.TryGetValue(MAPPERPATH, out object mapperPathObj);
            configDict.TryGetValue(USERMULTIPLEFS, out object userMFSObj);
            if (appNameObj != null && !string.IsNullOrWhiteSpace(appNameObj.ToString()))
            {
                appName = appNameObj.ToString();
            }
            if (mapperPathObj != null && !string.IsNullOrWhiteSpace(mapperPathObj.ToString()))
            {
                mapperPath = mapperPathObj.ToString();
                SqlMapperConfigure.DoInit(mapperPath);
            }
            if (userMFSObj != null && !string.IsNullOrWhiteSpace(userMFSObj.ToString()))
            {
                useMultipleFileSystem = string.Equals(userMFSObj.ToString(), "true", StringComparison.OrdinalIgnoreCase);
            }

            if (serverHostObj != null)
            {
                etcdHostUrl = serverHostObj.ToString();
                client = new(etcdHostUrl);
            }
            if (dataSourceDict != null)
            {
                Dictionary<object, object> sourceDict = dataSourceDict as Dictionary<object, object>;
                foreach (var entry in sourceDict)
                {
                    string key = entry.Key.ToString();
                    Dictionary<object, object> dict1 = entry.Value as Dictionary<object, object>;
                    DAOFactory.RegisterJdbcDao(key, dict1);
                }
            }
            //fileSystem 配置
            if (configDict.ContainsKey(FILESYSTEMS))
            {
                configDict.TryGetValue(FILESYSTEMS, out object fsParam);
                Dictionary<object, object> dict = fsParam as Dictionary<object, object>;
                if (!useMultipleFileSystem)
                {
                    AddFileSystemConfig(DEFAULT, dict);
                }
                else
                {
                    foreach (var entry in dict)
                    {
                        string fsName = entry.Key.ToString();
                        Dictionary<object, object> fsDict = entry.Value as Dictionary<object, object>;
                        AddFileSystemConfig(fsName, fsDict);
                    }
                }
            }

            if (client != null)
            {
                realIp = MachineUtils.GetRealLocalIP();
                etcdWorkerBasePath = etcdWorkerBasePath + "/worker/" + appName + "/" + realIp;
                cancellation = new();
                RegisterWorker();
            }

        }
        private void AddFileSystemConfig(string fsName, Dictionary<object, object> inputDict)
        {
            Dictionary<string, string> targetDict = [];
            foreach (var entry in inputDict)
            {
                targetDict.TryAdd(entry.Key.ToString(), entry.Value.ToString());
            }
            fileSystemConfig.TryAdd(fsName, targetDict);
        }
        public T GetConfig<T>(string key)
        {
            configDict.TryGetValue(key, out object retConfig);
            if (retConfig != null)
            {
                if (typeof(T) == retConfig.GetType())
                {
                    return (T)retConfig;
                }
                else
                {
                    Dictionary<object, object> targetDict = retConfig as Dictionary<object, object>;
                    dynamic retObj = Activator.CreateInstance<T>();
                    Dictionary<string, MethodParam> methodDict = AnnotationUtils.ReflectObject(typeof(T));
                    foreach (var entry in targetDict)
                    {
                        if (methodDict.ContainsKey(entry.Key.ToString()))
                        {
                            MethodParam param = methodDict[entry.Key.ToString()];
                            param.SetMethod.Invoke(retObj, new object[] { ConvertUtil.ParseByType(param.ParamType, entry.Value) });
                        }
                    }
                    return retObj;
                }
            }
            return default;
        }
        public Dictionary<string, string> GetFileSystemConfig(string name = null)
        {
            string fsName = name ?? DEFAULT;
            return fileSystemConfig[fsName];
        }
        private void RegisterWorker()
        {
            if (client != null)
            {
                //注册路径，KeepAlive 
                client.KeepAlive(etcdWorkerBasePath, ALIVE, cancellation.Token);
                notifyMaster();
                //监听发送的消息
                EtcdListener();
                //等待license 服务器响应是否允许运行，否则卡死
                WaitForMasterAck().ConfigureAwait(true);
                //验证license 是否有效，公钥验签
                if (!LicenseUtils.ValidateLicense())
                {
                    Trace.Fail("license faield!");
                    client.Dispose();
                    Environment.Exit(1);
                }
            }
        }
        private void notifyMaster()
        {
            using MemoryStream stream = new();
            using BinaryWriter write = new(stream);
            string systemTag = MachineUtils.GetSystemTag();
            write.Write(LicenseUtils.ACK);
            byte[] workerBytes = Encoding.UTF8.GetBytes(etcdWorkerBasePath);
            write.Write(workerBytes.Length);
            write.Write(workerBytes);
            byte[] systemTagBytes = Encoding.UTF8.GetBytes(systemTag);
            write.Write(systemTagBytes.Length);
            write.Write(systemTagBytes);
            //通知master上线
            client.PutValue(etcdmasterPath, stream.ToArray());
        }
        private async Task<bool> WaitForMasterAck()
        {
            await tcs.Task;
            return masterAck;
        }
        private void EtcdListener()
        {

            client.Watch(etcdWorkerBasePath, (response) =>
            {
                var events = response.Events;
                foreach (var evt in events)
                {
                    byte[] values = evt.Kv.Value.ToByteArray();
                    if (values.Equals(ALIVE))
                    {
                        Console.WriteLine("--ALIVE--");
                    }
                    if (values.Take(4).ToArray().Equals(LicenseUtils.PERMIT))
                    {
                        masterAck = true;
                        tcs.SetResult(true);
                    }
                    else if (values.Take(4).ToArray().Equals(LicenseUtils.BANNED))
                    {
                        Trace.Fail("worker banned!Service down");
                        client.Dispose();
                        Environment.Exit(1);
                    }
                    else if (values.Take(4).ToArray().Equals(LicenseUtils.EXPIRE))
                    {
                        Trace.Fail("license Expired!Service Down");
                        client.Dispose();
                        Environment.Exit(1);
                    }
                    else if (values.Take(4).ToArray().Equals(LicenseUtils.PUBLICKEYLIC))
                    {
                        //license 服务器发送随机公钥与证书文件，写入本地并回复服务器
                        WritePublicKeyAndLicense(values);
                    }
                }
            });
        }
        private void WritePublicKeyAndLicense(byte[] base64Content)
        {
            using MemoryStream stream = new MemoryStream(base64Content);
            using BinaryReader reader = new(stream);
            reader.ReadInt32();
            int length = reader.ReadInt32();
            byte[] publicKeybyte = new byte[length];
            reader.Read(publicKeybyte);
            WritePublicKey(publicKeybyte);
            length = reader.ReadInt32();
            byte[] licenseBytes = new byte[length];
            reader.Read(licenseBytes);
            WriteLicenseFile(licenseBytes);
            using MemoryStream mstream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(mstream);
            writer.Write(LicenseUtils.PUBLICKEYLICACK);
            writer.Write(Encoding.UTF8.GetBytes(realIp));
            //发送信号，等待服务器响应
            client.PutValue(etcdmasterPath, mstream.ToArray());
        }
        private void WritePublicKey(byte[] content)
        {
            string userPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName;
            string keyPath = userPath + Path.DirectorySeparatorChar + Environment.UserName + Path.DirectorySeparatorChar + ".robin" + Path.DirectorySeparatorChar + "publickey.pem";
            StringBuilder builder = new StringBuilder();
            builder.Append("-----BEGIN PUBLIC KEY-----").Append("\n");
            builder.Append(content);
            builder.Append("-----END PUBLIC KEY-----");
            using FileStream stream = File.OpenWrite(keyPath);
            stream.Write(Encoding.UTF8.GetBytes(builder.ToString()));

        }
        private void WriteLicenseFile(byte[] content)
        {
            string userPath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName;
            string licensePath = userPath + Path.DirectorySeparatorChar + Environment.UserName + Path.DirectorySeparatorChar + ".robin" + Path.DirectorySeparatorChar + ".license";
            using FileStream stream = File.OpenWrite(licensePath);
            stream.Write(content);
        }

        public void Dispose()
        {
            if (cancellation != null)
            {
                cancellation.Cancel();
            }
        }
    }
}
