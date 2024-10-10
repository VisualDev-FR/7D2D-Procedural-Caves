// Mock the game assembly for testing purpose

using System;


public static class Log
{
    public static void Out(object message)
    {
        Console.WriteLine($"{"INFO",-10} {message}");
    }


    public static void Error(string message)
    {
        Console.WriteLine($"{"ERROR",-10} {message}");
    }

    public static void Warning(string message)
    {
        Console.WriteLine($"{"WARNING",-10} {message}");
    }
}
