using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(WorldEnvironment), "Update")]
public class WorldEnvironment_Update
{
    private static readonly Vector2 dataAmbientInsideEquatorScale = WorldEnvironment.dataAmbientInsideEquatorScale;

    private static readonly Vector2 dataAmbientInsideGroundScale = WorldEnvironment.dataAmbientInsideGroundScale;

    private static readonly Vector2 dataAmbientInsideSkyScale = WorldEnvironment.dataAmbientInsideSkyScale;

    private static bool patchApplied = false;

    public static bool modActive = true;

    public static bool Prefix(WorldEnvironment __instance)
    {
        var localPlayer = __instance.localPlayer ?? GameManager.Instance.World.GetPrimaryPlayer();

        if (localPlayer == null)
            return true;

        var playerPosition = localPlayer.position;
        var terrainHeight = GameManager.Instance.World.GetHeightAt(playerPosition.x, playerPosition.z);

        if (modActive && terrainHeight > playerPosition.y)
        {
            ApplyCaveLighting(terrainHeight - playerPosition.y);
            patchApplied = true;
        }
        else if ((!modActive || playerPosition.y > terrainHeight) && patchApplied)
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
        // Log.Out($"[Cave] patch cave lighting, deep: {deep}");

        int deepThreshold = 10;

        if (deep >= deepThreshold)
        {
            WorldEnvironment.dataAmbientInsideEquatorScale = CaveConfig.CaveLightConfig.ambientInsideEquatorScale;
            WorldEnvironment.dataAmbientInsideGroundScale = CaveConfig.CaveLightConfig.ambientInsideGroundScale;
            WorldEnvironment.dataAmbientInsideSkyScale = CaveConfig.CaveLightConfig.ambientInsideSkyScale;
            return;
        }

        float ratio = 1 - (deep / deepThreshold);

        WorldEnvironment.dataAmbientInsideEquatorScale = LerpVector2(
            CaveConfig.CaveLightConfig.ambientInsideEquatorScale,
            dataAmbientInsideEquatorScale,
            ratio
        );

        WorldEnvironment.dataAmbientInsideEquatorScale = LerpVector2(
            CaveConfig.CaveLightConfig.ambientInsideGroundScale,
            dataAmbientInsideGroundScale,
            ratio
        );

        WorldEnvironment.dataAmbientInsideEquatorScale = LerpVector2(
            CaveConfig.CaveLightConfig.ambientInsideSkyScale,
            dataAmbientInsideSkyScale,
            ratio
        );

    }

    private static Vector2 LerpVector2(Vector2 min, Vector2 max, float ratio)
    {
        return new Vector2(
            Utils.FastLerpUnclamped(min.x, max.x, ratio),
            Utils.FastLerpUnclamped(min.y, max.y, ratio)
        );
    }
}
