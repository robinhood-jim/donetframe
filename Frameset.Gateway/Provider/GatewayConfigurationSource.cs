using System.Diagnostics;
using Consul;
using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Gateway.Common;
using Nacos.V2;

namespace Frameset.Gateway.Provider;

public class GatewayConfigurationSource : IConfigurationSource
{
    private readonly string discoveryType;
    private IConsulClient consulClient;
    private INacosConfigService nacosConfigService;
    private IConfigurationManager configuration;
    //using nacos as discovery
    public GatewayConfigurationSource(INacosConfigService nacosConfigService,IConfigurationManager configuration)
    {
        discoveryType = Constants.DISCOVERY_TYPE_NACOS;
        this.nacosConfigService = nacosConfigService;
        this.configuration = configuration;
    }
    //using consul as discovery
    public GatewayConfigurationSource(IConsulClient consulClient,IConfigurationManager configuration)
    {
        this.consulClient = consulClient;
        this.configuration = configuration;
        discoveryType = Constants.DISCOVERY_TYPE_CONSUL;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (string.Equals(Constants.DISCOVERY_TYPE_CONSUL, discoveryType, StringComparison.OrdinalIgnoreCase))
        {
            Trace.Assert(consulClient!=null,"missing consul config");
            string configKey = configuration.GetValue<string>(GatewayConstants.CONSUL_CONFIG_KEY);
            return new ConsulConfigurationProvider(consulClient, configKey);
        }else if (string.Equals(Constants.DISCOVERY_TYPE_NACOS, discoveryType, StringComparison.OrdinalIgnoreCase))
        {
            string dataId = configuration.GetValue<string>(GatewayConstants.NACOS_DATA_ID);
            string groupId = configuration.GetValue<string>(GatewayConstants.NACOS_GROUP_ID);
            return new NacosConfigurationProvider(nacosConfigService,dataId,groupId);
        }
        else
        {
            throw new ConfigIncorrectException($"missing discovery type {discoveryType}");
        }
    }
}