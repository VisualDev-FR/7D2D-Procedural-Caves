using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(WorldEnvironment), "Update")]
public static class WorldEnvironment_Update
{
    private static readonly Vector2 dataAmbientInsideEquatorScale = WorldEnvironment.dataAmbientInsideEquatorScale;

    private static readonly Vector2 dataAmbientInsideGroundScale = WorldEnvironment.dataAmbientInsideGroundScale;

    private static readonly Vector2 dataAmbientInsideSkyScale = WorldEnvironment.dataAmbientInsideSkyScale;

    private static bool patchApplied = false;

    public static bool Prefix(WorldEnvironment __instance)
    {
        var localPlayer = __instance.localPlayer ?? GameManager.Instance.World.GetPrimaryPlayer();

        if (localPlayer == null)
            return true;

        var playerPosition = localPlayer.position;
        var terrainHeight = GameManager.Instance.World.GetHeightAt(playerPosition.x, playerPosition.z);

        if (terrainHeight > playerPosition.y && !patchApplied)
        {
            ApplyCaveLighting(terrainHeight - playerPosition.y);
            patchApplied = true;
        }
        else if (playerPosition.y > terrainHeight && patchApplied)
        {
            ResetVanillaLighting();
            patchApplied = false;
        }

        return true;
    }

    public static void ResetVanillaLighting()
    {
        Log.Out("[Cave] reset vanilla lighting.");

        WorldEnvironment.dataAmbientInsideEquatorScale = dataAmbientInsideEquatorScale;
        WorldEnvironment.dataAmbientInsideGroundScale = dataAmbientInsideGroundScale;
        WorldEnvironment.dataAmbientInsideSkyScale = dataAmbientInsideSkyScale;
    }

    private static void ApplyCaveLighting(float deep)
    {
        Log.Out("[Cave] patch cave lighting");

        WorldEnvironment.dataAmbientInsideEquatorScale = CaveConfig.CaveLightConfig.ambientInsideEquatorScale;
        WorldEnvironment.dataAmbientInsideGroundScale = CaveConfig.CaveLightConfig.ambientInsideGroundScale;
        WorldEnvironment.dataAmbientInsideSkyScale = CaveConfig.CaveLightConfig.ambientInsideSkyScale;
    }
}