using Frameset.Core.Common;
using Frameset.Core.Reflect;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace Frameset.Core.Scripts
{
    public class ClearScriptUtils
    {
        private static readonly V8ScriptEngine engine = new V8ScriptEngine();
        private static SpinLock SpinLockInstance = new SpinLock(enableThreadOwnerTracking: false);
        static ClearScriptUtils()
        {
            engine.DocumentSettings.AccessFlags = DocumentAccessFlags.EnableFileLoading;
            engine.DefaultAccess = ScriptAccess.Full;
            engine.AddHostObject("host", new HostFunctions());
            engine.AddHostObject("lib", new HostTypeCollection("mscorlib", "System.Core"));
            engine.AddHostType("JsonSerializer", typeof(JsonSerializer));
        }

        public static V8ScriptEngine GetEngine()
        {
            return engine;
        }

        public static V8Script ReturnScript(V8ScriptEngine engine, string scriptContent)
        {
            return engine.Compile(scriptContent);
        }
        public static object Eval(V8Script script, Dictionary<string, object> map)
        {
            bool lockTaken = false;
            List<string> keys = [];
            try
            {
                SpinLockInstance.Enter(ref lockTaken);
                if (!map.IsNullOrEmpty())
                {

                    foreach (var item in map)
                    {
                        keys.Add(item.Key);
                        object value = item.Value;
                        if (value != null)
                        {
                            if (value.GetType().IsFundmentalType())
                            {
                                engine.Script[item.Key] = value;
                            }
                            else
                            {
                                engine.Script[item.Key] = WrapProperties(value);
                            }
                        }
                        else
                        {
                            engine.Script[item.Key] = null;
                        }
                    }
                }
                return engine.Evaluate(script);
            }
            finally
            {
                if (!keys.IsNullOrEmpty())
                {
                    foreach (string key in keys)
                    {
                        engine.Script[key] = null;
                    }
                }
                if (lockTaken)
                {
                    SpinLockInstance.Exit();
                }
            }

        }
        public static object Eval(V8Script script, string key, object input)
        {
            bool lockTaken = false;
            try
            {
                SpinLockInstance.Enter(ref lockTaken);
                engine.Script[key] = WrapProperties(input);
                return engine.Evaluate(script);
            }
            finally
            {
                engine.Script[key] = null;
                if (lockTaken)
                {
                    SpinLockInstance.Exit();
                }
            }
        }
        private static PropertyBag WrapProperties(object input)
        {
            var propertyBag = new PropertyBag();
            if (input != null)
            {
                if (input.GetType().IsTypeDictionary())
                {
                    Dictionary<string, object>? dict = input as Dictionary<string, object>;
                    if (dict != null && !dict.IsNullOrEmpty())
                    {
                        foreach (var item in dict)
                        {
                            object? value = item.Value;
                            if (value != null)
                            {
                                propertyBag.Add(item.Key, value);
                            }
                        }
                    }
                }
                else
                {
                    Dictionary<string, MethodParam> methodDict = AnnotationUtils.ReflectObject(input.GetType());
                    foreach (var item in methodDict)
                    {
                        object? value = item.Value.PropertyInfo.GetValue(input);
                        if (value != null)
                        {
                            propertyBag.Add(item.Key, value);
                        }
                    }
                }
            }
            return propertyBag;
        }
    }
}
