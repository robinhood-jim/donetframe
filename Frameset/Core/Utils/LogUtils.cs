using Serilog;

namespace Frameset.Core.Utils
{
    public sealed class LogUtils
    {
        public static void Debug(string message)
        {
            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                Log.Debug(message);
            }
        }
        public static void Debug(string messageTemplate, object[] parameters)
        {
            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                Log.Debug(messageTemplate, parameters);
            }
        }
        public static void Info(string message)
        {
            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Information))
            {
                Log.Information(message);
            }
        }
        public static void Info(string messageTemplate, object[] parameters)
        {
            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Information))
            {
                Log.Information(messageTemplate, parameters);
            }
        }
        public static void Error(string message)
        {
            if (Log.IsEnabled(Serilog.Events.LogEventLevel.Error))
            {
                Log.Error(message);
            }
        }
    }
}
