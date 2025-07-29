using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;


[HarmonyPatch]
public class H_Logging
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return typeof(Logging).GetMethod("Debug");
        yield return typeof(Logging).GetMethod("Info");
        yield return typeof(Logging).GetMethod("Warning");
        yield return typeof(Logging).GetMethod("Error");

        yield return typeof(Logging.Logger).GetMethod("Debug");
        yield return typeof(Logging.Logger).GetMethod("Info");
        yield return typeof(Logging.Logger).GetMethod("Warning");
        yield return typeof(Logging.Logger).GetMethod("Error");
    }

    private static string ObjectsToString(object[] objects)
    {
        return string.Join(" ", objects.Select(obj => obj?.ToString()));
    }

    public static bool Prefix(MethodBase __originalMethod, params object[] objects)
    {
        Console.WriteLine($"[{__originalMethod.Name.ToUpper()}] {ObjectsToString(objects)}");
        return false;
    }
}


[HarmonyPatch(typeof(ModConfig))]
[HarmonyPatch(MethodType.Constructor)]
[HarmonyPatch(new Type[] { typeof(int), typeof(bool) })]
class H_ModConfig
{
    public static bool Prefix(ModConfig __instance, int version, bool save)
    {
        var modConfigPath = Path.GetFullPath("../ModConfig.xml");

        if (!File.Exists(modConfigPath))
        {
            throw new FileNotFoundException($"Can't find ModConfig.xml");
        }

        var document = __instance.ReadXmlDocument(modConfigPath);
        var properties = __instance.ParseProperties(document);

        var bindingAttrs = BindingFlags.NonPublic | BindingFlags.Instance;

        typeof(ModConfig).GetField("document", bindingAttrs).SetValue(__instance, document);
        typeof(ModConfig).GetField("properties", bindingAttrs).SetValue(__instance, properties);

        return false;
    }
}