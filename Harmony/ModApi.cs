using System.Reflection;

public class ModAPI : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        var harmony = new HarmonyLib.Harmony(_modInstance.Name);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}