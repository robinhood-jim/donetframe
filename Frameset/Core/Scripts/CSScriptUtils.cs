using CSScriptLib;
using System;
using System.Collections.Generic;

namespace Frameset.Core.Scripts
{
    public class CSScriptUtils
    {
        public static MethodDelegate<object> ReturnDelegate<T>(string scriptContent)
        {

            var evaluator = CSScript.Evaluator
            .ReferenceDomainAssemblies()
            .ReferenceAssemblyOf<T>();
            Type type = typeof(T);
            string packageName = string.Empty;
            string className = string.Empty;
            bool useMap = false;
            if (type.Equals(typeof(Dictionary<string, object>)))
            {
                packageName = "System.Collections.Generic";
                className = "Dictionary<string, object>";
                useMap = true;
            }
            else
            {
                packageName = type.Namespace;
                className = type.Namespace + "." + type.Name;
            }
            MethodDelegate<object> compiledFormula = null;

            if (useMap)
            {
                compiledFormula = evaluator.CreateDelegate<object>(@$"
                using System;
                using {packageName};
                double ExecuteFormula({className} sourceMap) {{
                    var args=(Dictionary<string,dynamic>)sourceMap;
                    return (double)({scriptContent});
                }}
                ");
            }
            else
            {
                compiledFormula = evaluator.CreateDelegate<object>(@$"
                using System;
                using {packageName};
                double ExecuteFormula({className} args) {{
                    return (double)({scriptContent});
                }}
                ");
            }
            return compiledFormula;
        }



    }
}
