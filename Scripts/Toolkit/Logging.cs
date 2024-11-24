using System.Linq;


public enum LoggingLevel
{
    DEBUG,
    INFO,
    WARNING,
    ERROR,
    NONE,
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
        if (loggingLevel > LoggingLevel.DEBUG)
            return;

        Log.Out($"[{loggerName}] {ObjectsToString(objects)}");
    }

    public static void Info(params object[] objects)
    {
        if (loggingLevel > LoggingLevel.INFO)
            return;

        Log.Out($"[{loggerName}] {ObjectsToString(objects)}");
    }

    public static void Warning(params object[] objects)
    {
        if (loggingLevel > LoggingLevel.WARNING)
            return;

        Log.Warning($"[{loggerName}] {ObjectsToString(objects)}");
    }

    public static void Error(params object[] objects)
    {
        if (loggingLevel > LoggingLevel.ERROR)
            return;

        Log.Error($"[{loggerName}] {ObjectsToString(objects)}");
    }
}

