using Frameset.Common.FileSystem;
using Frameset.Core.Common;
using Frameset.Core.Configuration;
using Frameset.Core.Context;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Handler;
using Frameset.Core.FileSystem;
using Frameset.Core.Ioc;
using Frameset.Web.Common;
using Frameset.Web.Dao.Handler;
using Frameset.Web.Handler;
using Frameset.Web.Ioc;
using Frameset.Web.Middleware;
using Frameset.Web.Services;
using Frameset.Web.Utils;
using Frameset.WebMvc.Filter;
using Frameset.WebMvc.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;
using System.Text;

Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console(
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}").CreateLogger();
var builder = WebApplication.CreateBuilder(args);
//builder.Host.UseServiceProviderFactory(new CustomServiceProviderFactory());
builder.Services.AddHttpContextAccessor();
ProjectConfiguration configuration = builder.Services.UsingConfiguration("classpath:config.yml");//ProxySqlDialect.Init
builder.Services.AddSingleton(configuration);
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddCaptcha(builder.Configuration);

Dictionary<object, object>? fileSystemConfig = configuration.GetConfig<Dictionary<object, object>>("fileSystem");
Dictionary<string, string> configMap = [];
//FileSystem
IFileSystem? fileSystem = null;
if (fileSystemConfig != null && !fileSystemConfig.IsNullOrEmpty())
{
    fileSystemConfig.TryGetValue("fsType", out object? fsType);
    foreach (var item in fileSystemConfig)
    {
        if (item.Key != null && item.Value != null)
        {
            string? key = item.Key.ToString();
            string? value = item.Value.ToString();
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                configMap.TryAdd(key, value);
        }
    }
    string? fsTypeStr = fsType?.ToString();
    DataCollectionDefine define = DataCollectionBuilder.NewBuilder().FsType(Constants.FsTypeOf(fsTypeStr)).WithConfig(configMap).Build();
    fileSystem = FileSystemFactory.GetFileSystem(define);
    builder.Services.AddSingleton(typeof(IFileSystem), fileSystem);
}

//JWT
var JwtSecretkey = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Secret"] ?? "123456");
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(JwtSecretkey),
    ValidateIssuer = false,
    ValidateAudience = false,
    RequireExpirationTime = false,
    ValidateLifetime = true
};
builder.Services.AddSingleton(tokenValidationParameters);
//PasswordEncode register
builder.Services.AddSingleton(typeof(IPasswordEncoder), new Md5PasswordEncoder());
builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

    }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = tokenValidationParameters;
                x.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = async context =>
                    {
                        if (context.HttpContext.Items.TryGetValue("IsBypassRequest", out var isBypass) && isBypass is true)
                        {
                            return;
                        }
                        // 【策略 1】优先从受安全保护的原生 HttpOnly Cookie A 中读取完整的 JWT
                        if (context.Request.Cookies.TryGetValue("AuthToken", out string? cookieToken) && !string.IsNullOrEmpty(cookieToken))
                        {
                            context.Token = cookieToken;
                            return;
                        }
                        if (context.Request.Cookies.TryGetValue("sessionKey", out string? sessionId) && !string.IsNullOrEmpty(sessionId))
                        {
                            var redis = context.HttpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();
                            var redisDb = redis.GetDatabase();

                            // 去分布式 Redis 仓库中动态还原该指针名下的原始 JWT 字符串
                            string redisKey = $"user:jwt:{sessionId}";
                            string? redisToken = await redisDb.StringGetAsync(redisKey);

                            if (!string.IsNullOrEmpty(redisToken))
                            {
                                context.Token = redisToken; // 完美还原登录态，无感移交给 .NET 鉴权中心
                            }
                        }
                    }
                };

            });

//add Authorization
builder.Services.AddAuthorizationBuilder().AddPolicy(WebConstants.PERMISSION + ".ADMIN", policy => policy.RequireRole("ADMIN", "ORGADMIN"));
builder.Services.AddAuthorizationBuilder().AddPolicy(WebConstants.PERMISSION + ".SysUser", policy => policy.Requirements.Add(new PermissionRequirement("/sysUser", "system:user", ["get", "post", "put", "delete"])));
builder.Services.AddAuthorizationBuilder().AddPolicy(WebConstants.PERMISSION + ".SysOrg", policy => policy.Requirements.Add(new PermissionRequirement("/sysOrg", "system:org", ["get", "post", "put", "delete"])));
builder.Services.AddAuthorizationBuilder().AddPolicy(WebConstants.PERMISSION + ".SysRole", policy => policy.Requirements.Add(new PermissionRequirement("/sysRole", "system:role", ["get", "post", "put", "delete"])));
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<ISecurityUtils, SecurityUtils>();
builder.Services.AddSingleton<IMetaObjectHandler, ColumnAutoFillHandler>();
//DateTime Convert
CustomDateTimeConverter converter = new CustomDateTimeConverter();
builder.Services.AddSingleton(c => converter);
builder.Services.AddControllers(config => { config.Filters.Add<CorsFilter>(); });
// Add services to the container.
builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.Converters.Add(converter);
});

var serverSection = builder.Configuration.GetSection("Server");
if (serverSection != null)
{
    string? urls = serverSection["Urls"];
    if (!string.IsNullOrWhiteSpace(urls))
    {
        builder.WebHost.UseUrls(urls);
    }
}

builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddPolicy("Policy", policy =>
{
    policy.WithOrigins("http://localhost:1520").AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddOpenTelemetry().ConfigureResource(resource => resource.AddService("Frameset.WebMvc"))
    .WithTracing(traing => traing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddSource("Runtime.Sync.Tracer").AddSource("Dynamic.Database.Profiler"));
//.AddZipkinExporter(options => options.Endpoint = new Uri("http://localhost:9411/api/v2/spans")));
//.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:9411/api/v2/spans")));
builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, UniversalAutoWiredControllerActivator>());
//dynamic Ioc scanning ServiceAttribute
//builder.Services.AddBussiness();
builder.Services.AddCustomIoc();
var app = builder.Build();
RegServiceContext.SetContext(app.Services);
app.UseRouting();
app.UseStaticFiles();
app.MapHealthChecks("/health");

app.UseServerless();
app.UseMiddleware<SecurityBypassMiddleware>();
app.UseMiddleware<DeChunkerMiddleware>();
app.UseAuthorization();
app.UseCors("Policy");
//RegServiceContext.ScanServices(typeof(ServiceAttribute));
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
//DbOperationHacker.Initialize(httpContextAccessor);
var life = app.Services.GetRequiredService<IHostApplicationLifetime>();
life.ApplicationStopped.Register(() =>
{
    DbContextFactory.Dispose();
    DAOFactory.Dispose();
    configuration.Dispose();
});

//configure Datasource
app.Run();
