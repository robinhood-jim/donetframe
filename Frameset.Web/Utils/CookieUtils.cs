using System.Net;
using Microsoft.AspNetCore.Http;

namespace Frameset.Web.Utils;

public class CookieUtils
{
    public static void AddCookie(HttpRequest request,HttpResponse response,string name,string value,string? path=null,int ageTs=0,string? domain=null)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, 
            
            IsEssential = true 
        };
        if (!string.IsNullOrWhiteSpace(path))
        {
            cookieOptions.Path = path;
        }
        
        if (!string.IsNullOrWhiteSpace(domain))
        {
            cookieOptions.Domain = domain;
        }

        if (ageTs > 0)
        {
            DateTime dateTime=DateTime.Now;
            cookieOptions.Expires=dateTime.AddSeconds(ageTs);
        }
        response.Cookies.Append(name,value,cookieOptions);
    }

    public static string? GetCookie(HttpRequest request,string name)
    {
        return request.Cookies[name];
    }

    public static void DeleteCookie(HttpResponse response, string name)
    {
        response.Cookies.Delete(name);
    }
}