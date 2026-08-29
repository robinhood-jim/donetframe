namespace Frameset.Gateway.Models;

public class MicroserviceRegModel
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string HealthCheckUrl { get; set; } = string.Empty;
}