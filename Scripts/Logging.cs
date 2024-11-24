using System.Linq;

namespace TheDescent
{
    public enum LoggingLevel
    {
        DEBUG,
        INFO,
        WARNING,
        ERROR,
    }

    public class Log
    {
        public static LoggingLevel loggingLevel = LoggingLevel.DEBUG;

        public static string loggerName = "PoweredFlashlights";

        private static string ObjectsToString(object[] objects)
        {
            return string.Join(" ", objects.Select(obj => obj.ToString()));
        }

        public static void Debug(params object[] objects)
        {
            if (loggingLevel < LoggingLevel.DEBUG)
                return;

            global::Log.Out($"[{loggerName}] {ObjectsToString(objects)}");
        }

        public static void Info(params object[] objects)
        {
            if (loggingLevel < LoggingLevel.INFO)
                return;

            global::Log.Out($"[{loggerName}] {ObjectsToString(objects)}");
        }

        public static void Warning(params object[] objects)
        {
            if (loggingLevel < LoggingLevel.WARNING)
                return;

            global::Log.Warning($"[{loggerName}] {ObjectsToString(objects)}");
        }

        public static void Error(params object[] objects)
        {
            if (loggingLevel < LoggingLevel.ERROR)
                return;

            global::Log.Error($"[{loggerName}] {ObjectsToString(objects)}");
        }
    }
}
