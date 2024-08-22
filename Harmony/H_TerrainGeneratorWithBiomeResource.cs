using HarmonyLib;


[HarmonyPatch(typeof(TerrainGeneratorWithBiomeResource))]
[HarmonyPatch("GenerateTerrain")]
[HarmonyPatch(new[] { typeof(World), typeof(Chunk), typeof(GameRandom), typeof(Vector3i), typeof(Vector3i), typeof(bool), typeof(bool) })]
public class TerrainGeneratorWithBiomeResource_GenerateTerrain
{
    public static void Postfix(Chunk _chunk)
    {
        if (CaveGenerator.isEnabled)
        {
            CaveGenerator.GenerateCave(_chunk);
        }
    }
}