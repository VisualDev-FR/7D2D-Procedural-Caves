// mock the game assembly for testing purposes

using System;

public static class Log
{
    public static void Out(string message)
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

public static class WorldBuilder
{
    public static class Instance
    {
        public static int HalfWorldSize => CaveBuilder.worldSize / 2;

        public static float GetHeight(int x, int y)
        {
            return 255;
        }
    }
}