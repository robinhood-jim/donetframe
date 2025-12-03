using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Frameset.Core.Common
{
    public static class AppConfigurtaionServices
    {
        public static IConfiguration Configuration { get; set; } = new ConfigurationBuilder()
            .Add(new JsonConfigurationSource { Path = "appsettings.json", ReloadOnChange = true })
            //根据环境变量读取配置
            //.Add(new JsonConfigurationSource { Path = $"appsettings.{System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", ReloadOnChange = true })
            .Build();

        /// <summary> 
        /// 通过强类型绑定读取配置 
        /// </summary> 
        /// <typeparam name="Entity">要绑定的类型</typeparam> 
        /// <param name="keyPath">配置文件路径</param> 
        /// <returns>绑定的类</returns> 
        public static Entity GetSectionObject<Entity>(string keyPath = null) where Entity : new()
        {
            var entity = new Entity();
            if (string.IsNullOrEmpty(keyPath))
            {
                //将appsettings.json全部配置绑定到模型  
                Configuration.Bind(entity);
            }
            else
            {
                //将指定路径下的配置项绑定到模型 
                var section = Configuration.GetSection(keyPath);
                section.Bind(entity);
            }
            return entity;
        }

    }
}
