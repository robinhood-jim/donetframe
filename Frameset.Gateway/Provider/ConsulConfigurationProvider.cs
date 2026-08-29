using Consul;
using Frameset.Core.Utils;



namespace Frameset.Gateway.Provider;

public class ConsulConfigurationProvider : ConfigurationProvider
{
    private readonly string configKey;
    private ulong lastIndex = 0;
    private readonly string configType;
    
    private IConsulClient consulClient;

    public ConsulConfigurationProvider(IConsulClient consulClient, string configKey,string configType="json")
    {
        this.consulClient = consulClient;
        this.configKey = configKey;
        this.configType = configType;
    }

    public override void Load()
    {
        try
        {
            var res = consulClient.KV.Get(configKey).GetAwaiter().GetResult();
            if (res.Response != null)
            {
                lastIndex = res.LastIndex;
                //ParseAndDataUpdate(res.Response.Value);
                LoadConfigAsync().GetAwaiter().GetResult();
                LogUtils.Info("load configuration");
            }
        }
        catch (Exception ex)
        {
            
        }
        Task.Run(() => WatchConfigAsync());
    }
    private async Task WatchConfigAsync()
    {
        while (true)
        {
            try
            {
                // Long-polling: Consul blocks this socket connection until a change happens or timeout expires
                var options = new QueryOptions { WaitIndex = lastIndex, WaitTime = TimeSpan.FromMinutes(5) };
                var res = await consulClient.KV.Get(configKey, options);

                if (res.Response != null && res.LastIndex > lastIndex)
                {
                    lastIndex = res.LastIndex;
                    //ParseAndDataUpdate(res.Response.Value);
                    await LoadConfigAsync();
                    // Triggers Microsoft.Extensions.Configuration system to reload memory maps
                    OnReload(); 
                    LogUtils.Info("[Consul Config]: Configuration refreshed successfully.");
                }
                await Task.Delay(3000);
            }
            catch (Exception ex)
            {
                LogUtils.Error($"[Consul Config Watch Error]: {ex.Message}. Retrying in 10s...");
                await Task.Delay(10000);
            }
        }
    }
    private async Task LoadConfigAsync()
    {
        try
        {
            var res = await consulClient.KV.Get(configKey);
            if (res.Response != null)
            {
                lastIndex = res.LastIndex;
                using (var stream = new MemoryStream(res.Response.Value))
                {
                    Data = JsonConfigurationFileParser.Parse(stream);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Consul Config] 讀取配置失敗: {ex.Message}");
        }
    }
   
    internal static class JsonConfigurationFileParser
    {
        public static IDictionary<string, string> Parse(Stream stream)
        {
            // 符合 .NET 8 的內建 JSON 串流扁平化解析機制
            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
            return config.AsEnumerable().ToDictionary(k => k.Key, v => v.Value ?? string.Empty);
        }
    }
}
    
