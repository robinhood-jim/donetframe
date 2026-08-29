using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Frameset.Tracing.Tracer;
using Microsoft.AspNetCore.Http;
namespace Frameset.Tracing.Hacker;
using HarmonyLib;

public class DbOperationHacker
{
    private static IHttpContextAccessor? _httpContextAccessor;
    private static readonly Harmony _harmony = new("com.aspnet.optimized.db.hacker");
    private static readonly ActivitySource _source = new("Dynamic.Database.Profiler");
    private const string OTelActivityKey = "Current_DB_Hacked_Activity";
    public static ConcurrentDictionary<string,int> scanNamespaces = [];
    
    public static void Initialize(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            ScanAndPatchAssembly(assembly);
        }
    }

    private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        ScanAndPatchAssembly(args.LoadedAssembly);
    }

    private static void ScanAndPatchAssembly(Assembly assembly)
    {
        string? name = assembly.GetName().Name;
        
        
        // 严格过滤系统库、微软官方库以及 OTel 自身，防止对 runtime 基础组件进行不必要的扫描从而影响启动性能
        if (string.IsNullOrEmpty(name) || 
            name.StartsWith("System.") || 
            name.StartsWith("Microsoft.") || name.Contains("BouncyCastle") ||
            name.StartsWith("netstandard") || name.Contains("Serilog") ||
            name.Contains("OpenTelemetry") || name.Contains("Google")  || name.Contains("Frameset.Tracing") ||
            name.Contains("Harmony"))
        {
            return;
        }
        string? fullName = assembly.FullName;
        if (string.IsNullOrEmpty(fullName) || !scanNamespaces.TryAdd(fullName, 1))
        {
            return; 
        }

        try
        {
            var enumerable = assembly.GetExportedTypes().Where(t => !t.IsAbstract && typeof(DbCommand).IsAssignableFrom(t));
            Type? scanType = enumerable.FirstOrDefault();
            if (scanType != null)
            {
                Console.WriteLine("--- scan assembly "+fullName);
                Console.WriteLine($"Enhace {scanType.FullName}");
                
                var patchList = new (string MethodName, Type[] Args, bool IsProtected, string PostfixName)[]
                {
                    // Queries use the Reader Postfix
                    ("ExecuteDbDataReader", new[] { typeof(System.Data.CommandBehavior) }, true, nameof(AfterExecuteReader)),
                    ("ExecuteDbDataReaderAsync", new[] { typeof(System.Data.CommandBehavior), typeof(CancellationToken) }, true, nameof(AfterExecuteReader)),
                    
                    // Updates/Inserts use the Scalar/NonQuery Postfix
                    ("ExecuteNonQuery", Type.EmptyTypes, false, nameof(AfterExecuteNonQuery)),
                    ("ExecuteNonQueryAsync", new[] { typeof(CancellationToken) }, false, nameof(AfterExecuteNonQuery)),
                    
                    ("ExecuteScalar", Type.EmptyTypes, false, nameof(AfterExecuteNonQuery)),
                    ("ExecuteScalarAsync", new[] { typeof(CancellationToken) }, false, nameof(AfterExecuteNonQuery))
                };
                
                // Use LINQ to register the patches dynamically
                Array.ForEach(patchList, p => TryPatchDatabaseMethod(scanType, p.MethodName, p.Args, p.IsProtected, p.PostfixName));
                
            }
        }
        catch (NotSupportedException) {  }
        catch (Exception ex)
        {
            Console.WriteLine($"[Hacker Warning] 扫描程序集 {name} 失败: {ex.Message}");
        }
    }
   
    private static void TryPatchDatabaseMethod(Type commandType, string methodName, Type[] argumentTypes, bool isProtected, string postfixName)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        if (isProtected) flags |= BindingFlags.NonPublic;

        MethodInfo? targetMethod = commandType.GetMethod(methodName, flags, null, argumentTypes, null);
        if (targetMethod == null || targetMethod.IsAbstract) return;

        try
        {
            var prefix = new HarmonyMethod(typeof(DbOperationHacker).GetMethod(nameof(BeforeExecute)));
            var postfix = new HarmonyMethod(typeof(DbOperationHacker).GetMethod(postfixName));

            _harmony.Patch(targetMethod, prefix: prefix, postfix: postfix);
            Console.WriteLine($"  -> [探针激活] 已拦截: {commandType.Name}.{methodName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  -> [刺入失败] {commandType.Name}.{methodName}: {ex.Message}");
        }
    }
     public static void BeforeExecute(object __instance, MethodBase __originalMethod)
    {
        HttpContext? httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null) return; 
        try
        {
            if (__instance is DbCommand command)
            {
                string databaseName = command.Connection?.Database ?? "UnknownDB";
                Console.WriteLine($"enter {databaseName} {__originalMethod.Name}");
                // 将 Span 的名字定义为: "DatabaseName MethodName" (例如: orders_db ExecuteNonQuery)
                var dbActivity = _source.StartActivity($"{databaseName} {__originalMethod.Name}");
                if (dbActivity != null)
                {
                    dbActivity.SetTag("db.provider", command.GetType().FullName);
                    Trace(dbActivity,command);
                    httpContext.Items[OTelActivityKey] = dbActivity;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Interceptor Prefix Error]: {ex.Message}");
        }
    }

    public static void AfterExecuteReader(Exception __exception)
    {
        ProcessExceptionAndCloseSpan(__exception);
    }
    public static void AfterExecuteNonQuery(Exception? __exception, int __result)
    {
        HttpContext? httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext != null && httpContext.Items[OTelActivityKey] is Activity dbActivity)
        {
            // 如果没有抛出异常，可以顺便记录下这次修改了多少行数据
            if (__exception == null)
            {
                dbActivity.SetTag("db.rows_affected", __result);
            }
        }
        // 处理报错
        ProcessExceptionAndCloseSpan(__exception);
    }
    private static void ProcessExceptionAndCloseSpan(Exception? exception)
    {
        HttpContext? httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null) return;

        if (httpContext.Items[OTelActivityKey] is Activity dbActivity)
        {
            try
            {
                if (exception != null)
                {
                    // 💡 必须先显式改变 Status 为 Error。因为 OTel 的 RecordException 默认只记录事件日志，不改动 Span 的整体成功/失败标记
                    dbActivity.SetStatus(ActivityStatusCode.Error, exception.Message);
                    // 将异常序列化到追踪链的错误面板中
                    GlobalTracer.FailedTrace(dbActivity,exception);
                }
                else
                {
                    dbActivity.SetStatus(ActivityStatusCode.Ok);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Telemetry Hack Postfix Error]: {ex.Message}");
            }
            finally
            {
                dbActivity.Dispose();
                httpContext.Items.Remove(OTelActivityKey);
            }
        }
    }
    protected static void Trace(Activity? activity,DbCommand proxyCommand)
    {
        activity?.AddTag("sql", proxyCommand.CommandText);
        if (proxyCommand.Parameters.Count > 0)
        {
            activity?.AddTag("params", JsonSerializer.Serialize(GetParametersByName(proxyCommand)));
        }
    }
    protected static Dictionary<string, object> GetParametersByName(DbCommand proxyCommand)
    {
        DbParameterCollection parameters = proxyCommand.Parameters;
        Dictionary<string, object> parameterDict = [];
        for (int i = 0; i < parameters.Count; i++)
        {
            string paramName = string.IsNullOrEmpty(parameters[i].ParameterName) ? $"{i}" : parameters[i].ParameterName;
            string paramValue = parameters[i].Value == DBNull.Value || parameters[i].Value == null ? "NULL" : parameters[i].Value.ToString() ?? "";
            parameterDict.TryAdd(paramName, paramValue);
        }
        return parameterDict;
    }
}