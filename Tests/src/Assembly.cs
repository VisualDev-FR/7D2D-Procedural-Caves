// Mock the game assembly for testing purpose

using System;
using System.Collections.Generic;


public class Log
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

    public static List<string> SplitString(string input, char delimiter)
    {
        List<string> result = new List<string>();
        string currentSegment = "";

        foreach (char c in input)
        {
            if (c == delimiter)
            {
                // Si on rencontre le délimiteur, on ajoute le segment courant à la liste
                if (currentSegment.Length > 0)
                {
                    result.Add(currentSegment);
                    currentSegment = ""; // On réinitialise le segment
                }
            }
            else
            {
                currentSegment += c; // On construit le segment
            }
        }

        // Ajouter le dernier segment s'il existe (sans délimiteur à la fin)
        if (currentSegment.Length > 0)
        {
            result.Add(currentSegment);
        }

        return result;
    }

}
