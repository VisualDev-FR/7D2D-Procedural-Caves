using System.Reflection;

namespace Harmony
{
    public class TheDescent : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            var harmony = new HarmonyLib.Harmony(_modInstance.Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Logging.Warning("Hello TheDescent!");
        }
    }
}