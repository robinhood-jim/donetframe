using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using OpenTelemetry.Trace;

namespace Frameset.Tracing.Tracer;

public class DbTracer
{
    private static readonly ActivitySource DbSource = new ActivitySource("Framset.Core.DbTracer", "1.0.0");

    public static Activity? StartTrace(IDbCommand command)
    {
        var activity = DbSource.StartActivity(command.CommandText.Split(' ')[0], ActivityKind.Client);
        DbParameterCollection collection= (DbParameterCollection)command.Parameters;
        
        if (activity != null)
        {
            activity.SetTag("db.params", JsonSerializer.Serialize(GetParametersByPos(collection)));
            activity.SetTag("db.statement", command.CommandText);
        }
        return activity;
    }

    // 當資料庫執行成功時呼叫
    public static void EndTrace(Activity? activity)
    {
        activity?.Dispose();
    }

    // 當資料庫發生例外狀況時呼叫，確保 Zipkin 能顯示紅標錯誤
    public static void LogException(Activity? activity, Exception ex)
    {
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.RecordException(ex); // 紀錄詳細的 StackTrace 點
            activity.Dispose();
        }
    }
    public static Dictionary<string, object> GetParametersByName(DbParameterCollection parameters)
    {
        Dictionary<string, object> parameterDict = [];
        for (int i = 0; i < parameters.Count; i++)
        {
            parameterDict.TryAdd(parameters[i].ParameterName, parameters[i].Value);
        }

        return parameterDict;
    }
    public static Dictionary<int, object> GetParametersByPos(DbParameterCollection parameters)
    {
        Dictionary<int, object> parameterDict = [];
        for (int i = 0; i < parameters.Count; i++)
        {
            parameterDict.TryAdd(i, parameters[i].Value);
        }
        return parameterDict;
    }
}