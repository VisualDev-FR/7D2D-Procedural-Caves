using System.Reflection;
using System.IO;

namespace Harmony
{
    public class TheDescent : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            var harmony = new HarmonyLib.Harmony(_modInstance.Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            var prefab = new CavePrefabInterop()
            {
                pos_x = 600,
                pos_y = 60,
                pos_z = 6,
            };

            Log.Out($"[caves] try calling c++ dll at '{CppPlugin.dllName}'");
            Log.Out($"[caves] {Directory.GetCurrentDirectory()}");
            Log.Out($"[caves] addtion cpp 660 + 6 = {CppPlugin.Ajouter(660, 6)}");
            Log.Out($"[caves] ProcessCavePrefab = {CppPlugin.ProcessCavePrefab(ref prefab)}");
        }
    }
}

