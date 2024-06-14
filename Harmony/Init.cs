using System.Reflection;

namespace Harmony
{
    public class ProceduralCaves : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            var harmony = new HarmonyLib.Harmony(_modInstance.Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            GameManager.Instance.waitForTargetFPS.TargetFPS = 60;
        }
    }
}