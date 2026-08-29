using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Frameset.Tracing.Tracer;

public static class GlobalTracer
{
    private static readonly ActivitySource DynamicSource = new("Runtime.Sync.Tracer");
    
    public static object? StartTrace(string methodName)
    {
        var activity = DynamicSource.StartActivity(methodName);
        return activity; 
    }

    // End: 無論成功或失敗，Finally 區塊都會呼叫
    public static void EndTrace(object? activityObj)
    {
        if (activityObj!=null && activityObj is Activity activity)
        {
            // 結束當前 Span。
            // 執行緒會自動將 Activity.Current 的指標「倒回」上一層的父方法 Span。
            activity.Dispose(); 
        }
    }

    public static void FailedTrace(Activity? activity,Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        var tagsCollection = new ActivityTagsCollection
        {
            { "exception.type", ex.GetType().FullName },
            { "exception.message", ex.Message },
            { "exception.stacktrace", ex.StackTrace }
        };
        activity?.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, tagsCollection));
    }
}