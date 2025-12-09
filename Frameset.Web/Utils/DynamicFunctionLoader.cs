using Frameset.Common.Annotation;
using Frameset.Core.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;

namespace Frameset.Web.Utils
{
    public static class DynamicFunctionLoader
    {
        private static readonly Dictionary<string, Assembly> assemblyMap = [];
        private static readonly Dictionary<string, AssemblyLoadContext> loadContentMap = [];
        private static readonly Dictionary<string, MethodInfo> methodMap = [];
        private static readonly Dictionary<string, DynamicFunction> funcMap = [];
        private static readonly Dictionary<string, List<string>> regFuncMap = [];
        private static readonly Dictionary<string, object> noStaticObjectMap = [];
        private static readonly string DEFAULTALLOWMETHODS = "POST,GET";

        public static void RegisterFunction(this Stream input, string assemblyName)
        {
            AssemblyLoadContext assemblyContent = new AssemblyLoadContext(assemblyName, true);
            Assembly assembly = assemblyContent.LoadFromStream(input);
            loadContentMap.TryAdd(assemblyName, assemblyContent);
            assemblyMap.TryAdd(assemblyName, assembly);
            ScanPackage(assembly, assemblyName);

        }
        public static void DRegisterFunction(string assemblyName)
        {
            if (loadContentMap.TryGetValue(assemblyName, out AssemblyLoadContext? context) && context != null)
            {
                assemblyMap.Remove(assemblyName);
                context.Unload();
                loadContentMap.Remove(assemblyName);
                assemblyMap.Remove(assemblyName);
                if (regFuncMap.TryGetValue(assemblyName, out List<string>? funcs) && !funcs.IsNullOrEmpty())
                {
                    foreach (string funcName in funcs)
                    {
                        methodMap.Remove(funcName);
                        funcMap.Remove(funcName);
                        noStaticObjectMap.Remove(funcName);
                    }
                    regFuncMap.Remove(assemblyName);
                }
            }
        }
        public static object InvokeFunctionDynamic(HttpRequest request, HttpResponse response, string funcName)
        {
            object? retObj = null;
            if (funcMap.TryGetValue(funcName, out DynamicFunction? function))
            {
                if (!function.CanAccess(request.Method))
                {
                    return OutputErrMsg("call method " + request.Method.ToUpper() + " not allowed!");
                }
                Dictionary<string, string> queryMap = request.WrapRequest();
                Dictionary<string, object>? reqObj = [];
                List<object> reqParams = new();

                if (!function.Parameters.IsNullOrEmpty())
                {
                    if (string.Equals(request.Method, "post", StringComparison.OrdinalIgnoreCase) || string.Equals(request.Method, "put", StringComparison.OrdinalIgnoreCase))
                    {
                        string content = request.GetRequestContent().Result;
                        Trace.Assert(!content.IsNullOrEmpty() && content.StartsWith('{') && content.EndsWith('}'), "not a valid json");
                        reqObj = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    }
                    foreach (ServerlessParameter parameter in function.Parameters)
                    {
                        if (parameter.TargetType.IsPrimitive)
                        {
                            if (queryMap.TryGetValue(parameter.ParameterName, out string? reqValue))
                            {
                                reqParams.Add(ConvertUtil.ParseByType(parameter.TargetType, reqValue));
                            }
                            else if (reqObj != null && reqObj.TryGetValue(parameter.ParameterName, out object? reqValue1))
                            {
                                reqParams.Add(ConvertUtil.ParseByType(parameter.TargetType, reqValue1));
                            }
                        }
                        else if (parameter.TargetType == typeof(HttpRequest))
                        {
                            reqParams.Add(request);
                        }
                        else if (parameter.TargetType == typeof(HttpResponse))
                        {
                            reqParams.Add(response);
                        }
                        else if (reqObj != null && parameter.TargetType == typeof(Dictionary<string, object>))
                        {
                            reqParams.Add(reqObj);
                        }
                        else
                        {
                            object? targetObj = ApplicationContext.GetBean(parameter.TargetType);
                            if (targetObj != null)
                            {
                                reqParams.Add(targetObj);
                            }
                            else
                            {
                                return OutputErrMsg("parameter " + parameter.ParameterName + " with type" + parameter.TargetType + " not found!");
                            }
                        }
                    }
                }
                if (function.IsStatic)
                {
                    retObj = function.TargetMethod.Invoke(null, reqParams.ToArray());//[.. reqParams]
                }
                else
                {
                    if (!noStaticObjectMap.TryGetValue(funcName, out object? tmpObj))
                    {
                        ConstructorInfo? constructor = function.TaregetType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, new Type[] { });
                        tmpObj = constructor?.Invoke(null);
                        if (!function.InitFunc.IsNullOrEmpty() && tmpObj != null)
                        {
                            MethodInfo? methodInfo = function.TaregetType.GetMethod(function.InitFunc, new Type[] { typeof(string) });
                            if (methodInfo != null)
                            {
                                methodInfo.Invoke(tmpObj, new object[] { function.InitParam });
                            }
                            else
                            {
                                return OutputErrMsg("init method " + function.InitFunc + " not found!");
                            }
                        }
                        noStaticObjectMap.TryAdd(funcName, tmpObj);
                    }
                    retObj = function.TargetMethod.Invoke(tmpObj, reqParams.ToArray());
                }
                if (retObj != null)
                {
                    return OutputMsg(retObj);
                }
                else
                {
                    return OutputErrMsg("return value is null!");
                }
            }
            else
            {
                return OutputErrMsg("function " + funcName + " not found!");
            }
        }
        private static Dictionary<string, string> WrapRequest(this HttpRequest request)
        {
            Dictionary<string, string> querMap = [];
            IQueryCollection collections = request.Query;
            if (!collections.IsNullOrEmpty())
            {
                foreach (var entry in collections)
                {
                    querMap.TryAdd(entry.Key, entry.Value);
                }
            }
            return querMap;
        }
        private async static Task<string> GetRequestContent(this HttpRequest request)
        {
            StringBuilder builder = new StringBuilder();
            using (StreamReader reader = new StreamReader(request.Body))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    builder.Append(line);
                }
            }
            if (builder.Length > 0)
            {
                return builder.ToString();
            }
            return string.Empty;
        }
        private static object OutputErrMsg(string message)
        {
            Dictionary<string, object> errDict = [];
            errDict.TryAdd("code", 500);
            errDict.TryAdd("success", false);
            errDict.TryAdd("message", message);
            return errDict;
        }
        private static object OutputMsg(object message)
        {
            Dictionary<string, object> errDict = [];
            errDict.TryAdd("code", 200);
            errDict.TryAdd("success", true);
            errDict.TryAdd("data", message);
            return errDict;
        }
        private static void ScanPackage(Assembly assembly, string assemblyName)
        {
            Type[] types = assembly.GetTypes();
            List<string> funcs = new();
            if (types != null && types.Length > 0)
            {

                foreach (var type in types)
                {
                    MethodInfo[] methods = type.GetMethods();
                    if (methods.IsNullOrEmpty())
                    {
                        continue;
                    }
                    foreach (MethodInfo method in methods)
                    {
                        ServerlessFuncAttribute? attribute;
                        string funcName = null!;
                        string? initFunc = null;
                        string? initParam = null;
                        string allowMethods = null!;
                        Attribute? selAttribute = method.GetCustomAttribute(typeof(ServerlessFuncAttribute));
                        if (selAttribute != null)
                        {
                            attribute = selAttribute as ServerlessFuncAttribute;
                            funcName = attribute.Value ?? method.Name;
                            allowMethods = attribute.AllowMethods ?? DEFAULTALLOWMETHODS;
                            initFunc = attribute.InitFunc;
                            initParam = attribute.InitParameter;
                        }
                        if (!funcName.IsNullOrEmpty())
                        {
                            funcs.Add(funcName);
                            DynamicFunction function = new(funcName, type, method, allowMethods, method.IsStatic);
                            if (!initFunc.IsNullOrEmpty())
                            {
                                function.InitFunc = initFunc;
                            }
                            if (!initParam.IsNullOrEmpty())
                            {
                                function.InitParam = initParam;
                            }
                            methodMap.TryAdd(funcName, method);
                            ParameterInfo[] parameters = method.GetParameters();
                            if (parameters.Length > 0)
                            {
                                List<ServerlessParameter> serverlessParameters = new();
                                foreach (ParameterInfo parameter in parameters)
                                {
                                    ServerlessParameter serverlessParameter = new ServerlessParameter
                                    {
                                        ParameterName = parameter.Name,
                                        TargetType = parameter.ParameterType
                                    };
                                    serverlessParameters.Add(serverlessParameter);
                                }
                                function.Parameters = serverlessParameters;
                            }
                            if (funcMap.ContainsKey(funcName))
                            {
                                throw new InvalidOperationException("funcName " + funcName + " already defined!");
                            }
                            else
                            {
                                funcMap.TryAdd(funcName, function);
                            }

                        }

                    }

                }
            }
            if (!funcs.IsNullOrEmpty())
            {
                regFuncMap.TryAdd(assemblyName, funcs);
            }
        }
    }
}
