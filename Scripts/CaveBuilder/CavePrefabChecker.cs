using System.Linq;

public class CavePrefabChecker
{
    public static bool IsValid(PrefabData prefabData)
    {
        var result = true;

        if (!HasRequiredTags(prefabData))
        {
            Logging.Warning(SkippingBecause(prefabData.Name, $"missing cave type tag: {prefabData.Tags}"));
            result = false;
        }

        if (!ContainsCaveMarkers(prefabData))
        {
            Logging.Warning(SkippingBecause(prefabData.Name, "no cave marker was found."));
            result = false;
        }

        if (!PrefabMarkersAreValid(prefabData))
        {
            Logging.Warning(SkippingBecause(prefabData.Name, "at least one marker is invalid."));
            result = false;
        }

        if (HasOverlappingMarkers(prefabData))
        {
            Logging.Warning(SkippingBecause(prefabData.Name, "cave markers overlaps"));
            result = false;
        }

        return result;
    }

    private static bool HasOverlappingMarkers(PrefabData prefab)
    {
        var markers = prefab.POIMarkers
            .Where(marker => marker.tags.Test_AnySet(CaveConfig.tagCaveMarker))
            .ToList();

        for (int i = 0; i < markers.Count; i++)
        {
            for (int j = i + 1; j < markers.Count; j++)
            {
                var center1 = GraphNode.MarkerCenter(markers[i]);
                var center2 = GraphNode.MarkerCenter(markers[j]);

                if (center1.x == center2.x && center1.z == center2.z)
                {
                    return true;
                }
            }
        }

        return false;
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
                Logging.Warning($"cave marker out of bounds: [{marker.start}] '{prefab.Name}'");
                return false;
            }

            // TODO: check 3D Intersection between prefab and markers
        }

        return true;
    }

    private static string SkippingBecause(string prefabName, string reason)
    {
        return $"skipping '{prefabName}' because {reason}.";
    }

}