using System.Collections.Generic;
using HarmonyLib;


[HarmonyPatch(typeof(GameManager), "createWorld")]
public static class GameManager_createWorld
{
    public static bool Prefix(string _sWorldName, string _sGameName, List<WallVolume> _wallVolumes, bool _fixedSizeCC = false)
    {
        CaveGenerator.LoadCaveMap(_sWorldName);
        return true;
    }
}
