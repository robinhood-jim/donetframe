using Consul;
using Frameset.Core.Context;
using Frameset.Gateway.Provider;

namespace Frameset.Gateway.Extension;

public static class ConfigurationManagerExtensions
{
    public static ConfigurationManager AddConsulConfiguration(this ConfigurationManager configurationManager, string consulUrl, string configKey)
    {
        IConfigurationBuilder builder = configurationManager;

        builder.Add(new GatewayConfigurationSource(RegServiceContext.GetBean<IConsulClient>(), configurationManager));
        return configurationManager;
    }
}