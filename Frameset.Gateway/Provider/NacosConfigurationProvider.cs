using System.Text;
using Nacos.V2;
using Serilog;

namespace Frameset.Gateway.Provider;

public class NacosConfigurationProvider:ConfigurationProvider
{
    private readonly INacosConfigService configService;
    private readonly string dataId;
    private readonly string groupId;
    public NacosConfigurationProvider(INacosConfigService configService,string dataId,string groupId)
    {
        this.configService = configService;
        this.dataId = dataId;
        this.groupId = groupId;
    }

    public override void Load()
    {
        try
        {
            string config = configService.GetConfig(dataId, groupId, 5000).GetAwaiter().GetResult();
            LoadConfiguration(config);
            configService.AddListener(dataId,groupId,new NacosConfigListener(this)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }
    private void LoadConfiguration(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
            Data = config.AsEnumerable().ToDictionary(k => k.Key, v => v.Value ?? string.Empty);
            OnReload();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }
    private class NacosConfigListener : IListener
    {
        private readonly NacosConfigurationProvider _provider;
        public NacosConfigListener(NacosConfigurationProvider provider) => _provider = provider;

        public void ReceiveConfigInfo(string configInfo) => _provider.LoadConfiguration(configInfo);
    }
}