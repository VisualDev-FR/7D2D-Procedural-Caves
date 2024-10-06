// // Mock the game assembly for testing purpose

// # pragma warning disable CA1050, CA2211, IDE0290, IDE0060

// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Runtime.CompilerServices;


// public static class Log
// {
//     public static void Out(object message)
//     {
//         Console.WriteLine($"{"INFO",-10} {message}");
//     }


//     public static void Error(string message)
//     {
//         Console.WriteLine($"{"ERROR",-10} {message}");
//     }

//     public static void Warning(string message)
//     {
//         Console.WriteLine($"{"WARNING",-10} {message}");
//     }
// }


// namespace WorldGenerationEngineFinal
// {
//     public class WorldBuilder
//     {
//         public static class Instance
//         {
//             public static bool IsCanceled = false;

//             public static int GetHeight(int x, int z)
//             {
//                 return 128;
//             }

//             public static IEnumerator SetMessage(string message, bool toLogConsole = false)
//             {
//                 Log.Out(message);
//                 yield return null;
//             }
//         };
//     }
// }
