using Microsoft.ClearScript.V8;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace Frameset.Core.Scripts
{
    public class ClearScriptUtils
    {
        public static V8ScriptEngine GetEngine()
        {
            var engine = new V8ScriptEngine();
            engine.DocumentSettings.AccessFlags = Microsoft.ClearScript.DocumentAccessFlags.EnableFileLoading;
            engine.DefaultAccess = Microsoft.ClearScript.ScriptAccess.Full;
            return engine;
        }

        public static V8Script ReturnScript(V8ScriptEngine engine, string scriptContent)
        {

            return engine.Compile(scriptContent);

        }
        public static object Eval(V8ScriptEngine engine, V8Script script, Dictionary<string, object> map)
        {

            if (!map.IsNullOrEmpty())
            {
                foreach (var item in map)
                {
                    engine.Script[item.Key] = item.Value;
                }
            }
            return engine.Evaluate(script);
        }
    }
}
