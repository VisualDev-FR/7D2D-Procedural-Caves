using System.Linq;


public enum LoggingLevel
{
    DEBUG,
    INFO,
    WARNING,
    ERROR,
}

public class Logging
{
    public static LoggingLevel loggingLevel = LoggingLevel.DEBUG;

    public static string loggerName = "Cave";

    private static string ObjectsToString(object[] objects)
    {
        return string.Join(" ", objects.Select(obj => obj.ToString()));
    }

    public static void Debug(params object[] objects)
    {
        if (loggingLevel < LoggingLevel.DEBUG)
            return;

        global::Logging.Info($"[{loggerName}] {ObjectsToString(objects)}");
    }

    public static void Info(params object[] objects)
    {
        if (loggingLevel < LoggingLevel.INFO)
            return;

        global::Logging.Info($"[{loggerName}] {ObjectsToString(objects)}");
    }

    public static void Warning(params object[] objects)
    {
        if (loggingLevel < LoggingLevel.WARNING)
            return;

        global::Logging.Warning($"[{loggerName}] {ObjectsToString(objects)}");
    }

    public static void Error(params object[] objects)
    {
        if (loggingLevel < LoggingLevel.ERROR)
            return;

        global::Logging.Error($"[{loggerName}] {ObjectsToString(objects)}");
    }
}

