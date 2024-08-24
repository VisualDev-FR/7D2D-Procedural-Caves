public class CavePrefabChecker
{
    public static bool IsValid(PrefabData prefabData)
    {
        if (!HasRequiredTags(prefabData))
        {
            Log.Warning(SkippingBecause(prefabData.Name, $"missing cave type tag: {prefabData.Tags}"));
            return false;
        }

        if (!ContainsCaveMarkers(prefabData))
        {
            Log.Warning(SkippingBecause(prefabData.Name, "no cave marker was found."));
            return false;
        }

        if (!PrefabMarkersAreValid(prefabData))
        {
            Log.Warning(SkippingBecause(prefabData.Name, "at least one marker is invalid."));
            return false;
        }

        return true;
    }

    private static bool HasRequiredTags(PrefabData prefab)
    {
        return prefab.Tags.Test_AnySet(CaveConfig.requiredCaveTags);
    }

    private static bool ContainsCaveMarkers(PrefabData prefab)
    {
        foreach (var marker in prefab.POIMarkers)
        {
            if (marker.tags.Test_AnySet(CaveConfig.tagCaveMarker))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PrefabMarkersAreValid(PrefabData prefab)
    {
        foreach (var marker in prefab.POIMarkers)
        {
            if (!marker.tags.Test_AnySet(CaveConfig.tagCaveMarker))
                continue;

            bool isOnBound_x = marker.start.x == -1 || marker.start.x == prefab.size.x;
            bool isOnBound_z = marker.start.z == -1 || marker.start.z == prefab.size.z;

            if (!isOnBound_x && !isOnBound_z)
            {
                Log.Out($"[Cave] cave marker out of bounds: [{marker.start}] '{prefab.Name}'");
                return false;
            }

            // TODO: check 3D Intersection between prefab and markers
        }

        return true;
    }

    private static string SkippingBecause(string prefabName, string reason)
    {
        return $"[Cave] skipping '{prefabName}' because {reason}.";
    }

}