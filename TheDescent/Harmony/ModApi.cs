using System.Reflection;

public class ModAPI : IModApi
{
    public void InitMod(Mod mod)
    {
        new HarmonyLib.Harmony(mod.Name).PatchAll(Assembly.GetExecutingAssembly());
    }
}