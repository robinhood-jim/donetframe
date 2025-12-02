using Microsoft.AspNetCore.Mvc;

namespace Frameset.Web.Controller
{
    public class AbstractControllerBase : ControllerBase
    {
        public static JsonResult OutputErrMsg(string message)
        {
            Dictionary<string, object> errDict = [];
            errDict.TryAdd("code", 500);
            errDict.TryAdd("success", false);
            errDict.TryAdd("message", message);
            return new JsonResult(errDict);
        }
        public static JsonResult OutputMsg(object message)
        {
            Dictionary<string, object> outputDict = [];
            outputDict.TryAdd("code", 200);
            outputDict.TryAdd("success", true);
            outputDict.TryAdd("data", message);
            return new JsonResult(outputDict);
        }
    }
}
