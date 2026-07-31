namespace Frameset.Gateway.Models;

public class RouteDefine
{
    public string Id { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty; // 目標微服務網址或 consul://ServiceName
    public List<string> Predicates { get; set; } = new(); // 路由斷言，如 Path=/order/**
    public List<string> Filters { get; set; } = new(); 
    public string PathPattern { get; set; } = string.Empty;
    public bool StripPrefix { get; set; } = true; 
}