namespace Frameset.Gateway.Models;

public class GatewayConfig
{
    public string ServiceId
    {
        get;
        set;
    } = "api-gateway-domain";
    public string ServiceName
    {
        get;
        set;
    } = "api-gateway";
    public string ServiceIp
    {
        get;
        set;
    } = "127.0.0.1";

    public int ServicePort
    {
        get;
        set;
    } = 8900;

    public List<ServicesModel> Services
    {
        get;
        set;
    } = [];

    public List<RouteDefine> Routes
    {
        get;
        set;
    } = [];
}