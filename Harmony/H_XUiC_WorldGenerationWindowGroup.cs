using HarmonyLib;
using WorldGenerationEngineFinal;

public class H_XUiC_WorldGenerationWindowGroup
{
    public static XUiC_ComboBoxInt terrainOffset;

    public static XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections> caveNetworks;

    public static XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections> caveEntrances;

    public static XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections> caveWater;

    public static int TerrainOffset => (int)terrainOffset.Value;

    public static WorldBuilder.GenerationSelections CaveNetworks => caveNetworks.Value;

    public static WorldBuilder.GenerationSelections CaveEntrances => caveEntrances.Value;

    public static WorldBuilder.GenerationSelections CaveWater => caveWater.Value;
}


[HarmonyPatch(typeof(XUiC_WorldGenerationWindowGroup), "OnOpen")]
public class XUiC_WorldGenerationWindowGroup_OnOpen
{
    public static void Postfix(XUiC_WorldGenerationWindowGroup __instance)
    {
        if ((H_XUiC_WorldGenerationWindowGroup.terrainOffset = __instance.GetChildById("terrainOffset") as XUiC_ComboBoxInt) != null)
        {
            H_XUiC_WorldGenerationWindowGroup.terrainOffset.Value = (int)CaveConfig.terrainOffset;
        }

        if ((H_XUiC_WorldGenerationWindowGroup.caveNetworks = __instance.GetChildById("caveNetworks") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>) != null)
        {
            H_XUiC_WorldGenerationWindowGroup.caveNetworks.Value = WorldBuilder.GenerationSelections.Default;
        }

        if ((H_XUiC_WorldGenerationWindowGroup.caveEntrances = __instance.GetChildById("caveEntrances") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>) != null)
        {
            H_XUiC_WorldGenerationWindowGroup.caveEntrances.Value = WorldBuilder.GenerationSelections.Default;
        }

        if ((H_XUiC_WorldGenerationWindowGroup.caveWater = __instance.GetChildById("caveWater") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>) != null)
        {
            H_XUiC_WorldGenerationWindowGroup.caveWater.Value = WorldBuilder.GenerationSelections.Default;
        }
    }
}


[HarmonyPatch(typeof(XUiC_WorldGenerationWindowGroup), "Generate")]
public class XUiC_WorldGenerationWindowGroup_Generate
{
    public static bool Prefix(bool _usePreviewer = true)
    {
        CaveConfig.terrainOffset = H_XUiC_WorldGenerationWindowGroup.TerrainOffset;
        CaveConfig.caveNetworks = H_XUiC_WorldGenerationWindowGroup.CaveNetworks;
        CaveConfig.caveEntrances = H_XUiC_WorldGenerationWindowGroup.CaveEntrances;
        CaveConfig.caveWater = H_XUiC_WorldGenerationWindowGroup.CaveWater;

        CaveConfig.generateWater = CaveConfig.caveWater != WorldBuilder.GenerationSelections.None;
        CaveConfig.generateCaves = CaveConfig.caveNetworks != WorldBuilder.GenerationSelections.None;

        // TODO: remove this line when water will be debugged
        CaveConfig.generateWater = false;

        Log.Out($"[CaveConfig] generateWater: {CaveConfig.generateWater}");
        Log.Out($"[CaveConfig] terrainOffset: {CaveConfig.terrainOffset}");

        return true;
    }
}
