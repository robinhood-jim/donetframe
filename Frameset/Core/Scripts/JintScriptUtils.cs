using Frameset.Core.Reflect;
using Jint;
using Jint.Native;
using System;
using System.Collections.Generic;


namespace Frameset.Core.Scripts
{
    public class JintScriptUtils
    {
        public static Engine GetEngine()
        {
            return new Engine();
        }
        public static Prepared<Acornima.Ast.Script> PreparedScript(string scriptContent)
        {
            return Engine.PrepareScript(scriptContent);
        }
        public static JsValue Eval<T>(Engine engine, Prepared<Acornima.Ast.Script> script, T input)
        {
            Type type = typeof(T);
            bool useMap = type.Equals(typeof(Dictionary<string, object>));
            if (useMap)
            {
                Dictionary<string, object> dict = input as Dictionary<string, object>;
                foreach (var item in dict)
                {
                    engine.SetValue(item.Key, item.Value);
                }
            }
            else
            {
                Dictionary<string, MethodParam> dict = AnnotationUtils.ReflectObject(type);
                foreach (var item in dict)
                {
                    object value = item.Value.GetMethod.Invoke(input, []);
                    if (value != null)
                    {
                        engine.SetValue(item.Key, value);
                    }
                }

            }
            return engine.Evaluate(script);
        }

    }
}
