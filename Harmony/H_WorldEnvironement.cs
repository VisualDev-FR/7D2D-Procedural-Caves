using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(WorldEnvironment), "AmbientSpectrumFrameUpdate")]
public class WorldEnvironment_AmbientSpectrumFrameUpdate
{
    private static Vector2 dataAmbientSkyDesat => WorldEnvironment.dataAmbientSkyDesat;

    private static Vector2 dataAmbientMoon => WorldEnvironment.dataAmbientMoon;

    private static Vector2 dataAmbientSkyScale => WorldEnvironment.dataAmbientSkyScale;

    private static Vector2 dataAmbientInsideSkyScale => WorldEnvironment.dataAmbientInsideSkyScale;

    private static Vector2 dataAmbientEquatorScale => WorldEnvironment.dataAmbientEquatorScale;

    private static Vector2 dataAmbientInsideEquatorScale => WorldEnvironment.dataAmbientInsideEquatorScale;

    private static Vector2 dataAmbientGroundScale => WorldEnvironment.dataAmbientGroundScale;

    private static Vector2 dataAmbientInsideGroundScale => WorldEnvironment.dataAmbientInsideGroundScale;

    private static float dataAmbientInsideThreshold => WorldEnvironment.dataAmbientInsideThreshold;

    private static float dataAmbientInsideSpeed => WorldEnvironment.dataAmbientInsideSpeed;

    private static float deepCurrentState = 1f;

    private static float AmbientTotal
    {
        set => WorldEnvironment.AmbientTotal = value;
    }

    public static bool Prefix(WorldEnvironment __instance)
    {
        var world = __instance.world;
        var localPlayer = __instance.localPlayer ?? world.GetPrimaryPlayer();
        var indoorCurrentState = __instance.insideCurrent;
        var nightVisionEffect = __instance.nightVisionBrightness;

        if (world is null || world.BiomeAtmosphereEffects is null)
            return false;

        // Determine if the player is indoors based on light exposure
        float targetIndoorState = 0f;
        if ((bool)localPlayer && localPlayer.Stats.LightInsidePer >= dataAmbientInsideThreshold)
        {
            targetIndoorState = 1f;
        }

        float targetDeepState = 1f;
        if (localPlayer != null)
        {
            Vector3 playerPosition = localPlayer.position;
            float terrainHeight = GameManager.Instance.World.GetHeightAt(playerPosition.x, playerPosition.z);
            float depth = Utils.FastMax(0, terrainHeight - playerPosition.y + 0.5f);

            targetDeepState = 1f - (depth / 16);

            deepCurrentState = Mathf.MoveTowards(deepCurrentState, targetDeepState, 1f * Time.deltaTime);
        }

        // Smoothly interpolate the "indoor" state for the player
        indoorCurrentState = Mathf.MoveTowards(indoorCurrentState, targetIndoorState, dataAmbientInsideSpeed * Time.deltaTime);

        // Get the current day percentage (0 = midnight, 1 = next midnight)
        float dayProgress = SkyManager.dayPercent;

        // Get the current sky color based on the time of day
        Color skyColor = SkyManager.GetSkyColor();

        // Get the player's graphics brightness setting and ensure a minimum value
        float graphicsBrightness = GamePrefs.GetFloat(EnumGamePrefs.OptionsGfxBrightness) + 0.5f;
        if (graphicsBrightness < 1f)
        {
            graphicsBrightness = 1f;
        }

        // Calculate the ambient light contribution from the moon
        float moonLightScale = SkyManager.GetMoonAmbientScale(dataAmbientMoon.x, dataAmbientMoon.y);
        moonLightScale = Mathf.LerpUnclamped(moonLightScale, 1f, indoorCurrentState);

        // Combine moonlight with graphics brightness and night vision effect
        graphicsBrightness *= moonLightScale;
        graphicsBrightness += nightVisionEffect;

        // Determine the sky desaturation based on the day progress
        float skyDesaturationFactor = Mathf.LerpUnclamped(dataAmbientSkyDesat.y, dataAmbientSkyDesat.x, dayProgress);
        Color desaturatedSkyColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Neutral gray for desaturation
        Color blendedSkyColor = Color.LerpUnclamped(skyColor, desaturatedSkyColor, skyDesaturationFactor);

        // Adjust sky brightness based on indoor/outdoor and time of day
        float outdoorSkyScale = Mathf.LerpUnclamped(dataAmbientSkyScale.y, dataAmbientSkyScale.x, dayProgress);
        float indoorSkyScale = Mathf.LerpUnclamped(dataAmbientInsideSkyScale.y, dataAmbientInsideSkyScale.x, dayProgress);
        float skyBrightnessScale = Mathf.LerpUnclamped(outdoorSkyScale, indoorSkyScale, indoorCurrentState);

        // Combine sky color with brightness scale
        float skyLuminance = SkyManager.GetLuma(blendedSkyColor) * skyBrightnessScale;
        skyBrightnessScale *= graphicsBrightness;
        RenderSettings.ambientSkyColor = blendedSkyColor * skyBrightnessScale * deepCurrentState;

        // Adjust equator brightness (horizon line)
        float outdoorEquatorScale = Mathf.LerpUnclamped(dataAmbientEquatorScale.y, dataAmbientEquatorScale.x, dayProgress);
        float indoorEquatorScale = Mathf.LerpUnclamped(dataAmbientInsideEquatorScale.y, dataAmbientInsideEquatorScale.x, dayProgress);
        float equatorBrightnessScale = Mathf.LerpUnclamped(outdoorEquatorScale, indoorEquatorScale, indoorCurrentState);
        equatorBrightnessScale *= graphicsBrightness;
        RenderSettings.ambientEquatorColor = SkyManager.GetFogColor() * equatorBrightnessScale * deepCurrentState;

        // Adjust ground brightness based on sunlight and indoor/outdoor state
        Color sunlightColor = SkyManager.GetSunLightColor();
        float outdoorGroundScale = Mathf.LerpUnclamped(dataAmbientGroundScale.y, dataAmbientGroundScale.x, dayProgress);
        float indoorGroundScale = Mathf.LerpUnclamped(dataAmbientInsideGroundScale.y, dataAmbientInsideGroundScale.x, dayProgress);
        float groundBrightnessScale = Mathf.LerpUnclamped(outdoorGroundScale, indoorGroundScale, indoorCurrentState);

        // Combine ground luminance and graphics brightness
        skyLuminance += SkyManager.GetLuma(sunlightColor) * groundBrightnessScale;
        groundBrightnessScale *= graphicsBrightness;
        RenderSettings.ambientGroundColor = sunlightColor * groundBrightnessScale * deepCurrentState;

        // Update the total ambient light value for other systems
        AmbientTotal = skyLuminance * moonLightScale;

        return false;
    }
}
