using HarmonyLib;

[HarmonyPatch(typeof(SkyManager), "Update")]
public class SkyManager_Update
{
    public static void Postfix()
    {
        SkyManager.moonBright *= CaveConfig.moonLightScale;
    }
}