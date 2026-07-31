using Consul;
using Frameset.Core.Context;
using Frameset.Gateway.Common;
using Frameset.Gateway.Middleware;
using Frameset.Gateway.Models;
using Frameset.Gateway.Provider;
using Frameset.Gateway.Resolver;
using Frameset.Gateway.Worker;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
var consulUrl = builder.Configuration[GatewayConstants.CONSUL_URL] ?? "http://localhost:8500";
ConsulClient consulClient = new ConsulClient(c => c.Address = new Uri(consulUrl));
builder.Services.AddSingleton<IConsulClient, ConsulClient>(sp =>consulClient);
builder.Services.Configure<GatewayConfig>(builder.Configuration.GetSection("GatewayConfig"));
builder.Services.AddSingleton<GatewayConsulRouterResolver>();
((IConfigurationBuilder)builder.Configuration).Add(
    new GatewayConfigurationSource(consulClient,builder.Configuration)
);

// 2. Add dependencies 
builder.Services.AddSingleton<IConsulClient>(sp => new ConsulClient(c =>
{
    c.Address = new Uri(consulUrl);
}));
builder.Services.AddHostedService<ConsulRegisterWorker>();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();
string urls = builder.Configuration.GetValue<string>("server.urls");
builder.WebHost.UseUrls(urls);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseMiddleware<GatewayMiddleware>();
app.MapHealthChecks("/health");
RegServiceContext.SetContext(app.Services);
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();