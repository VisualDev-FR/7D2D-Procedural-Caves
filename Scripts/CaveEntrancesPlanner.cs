/*
    refactored WildernessPlanner, allowing more control on cave entrances placement
*/

using System.Collections.Generic;
using WorldGenerationEngineFinal;
using UnityEngine;
using System.Linq;
using System;


public static class CaveEntrancesPlanner
{
    private static GameRandom gameRandom;

    public static void SpawnCaveEntrances(GameRandom _gameRandom)
    {
        gameRandom = _gameRandom;

        var spawnedEntrances = new List<PrefabData>();
        var wildernessTiles = GetShuffledWildernessTiles();
        var maxEntrancesCount = wildernessTiles.Count;
        var maxRolls = 1_000;
        var tileIndex = 0;
        var usedTileIndexes = new HashSet<int>();

        if (wildernessTiles.Count == 0)
        {
            Log.Error("[Cave] No wilderness streetTile was found");
            return;
        }

        while (usedTileIndexes.Count < maxEntrancesCount && --maxRolls > 0)
        {
            var tile = wildernessTiles[tileIndex % wildernessTiles.Count];
            var prefab = CavePlanner.SelectRandomWildernessEntrance();
            var succeed = SpawnWildernessCaveEntrance(tile, prefab);

            if (succeed)
            {
                Log.Out($"[Cave] Entrance spawned: '{prefab.Name}' on tile '{tile.GridPosition}'");
                spawnedEntrances.Add(prefab);
                usedTileIndexes.Add(tileIndex);
                tileIndex++;
            }
        }

        return;
    }

    private static bool IsWildernessStreetTile(StreetTile st)
    {
        return
            !st.OverlapsRadiation
            && !st.AllIsWater
            && !st.Used
            && !st.ContainsHighway
            && !st.HasPrefabs
            // && !WildernessPlanner.hasPrefabNeighbor(st)
            && (st.District == null || st.District.name == "wilderness");
    }

    private static List<StreetTile> GetShuffledWildernessTiles()
    {
        var result =
            from StreetTile st in WorldBuilder.Instance.StreetTileMap
            where IsWildernessStreetTile(st)
            select st;

        return result
            .OrderBy(tile => gameRandom.Next())
            .ToList();
    }

    private static Vector2i GetRandomPosition(int sizeX, int sizeZ, Vector2i worldPositionCenter, Vector2i worldPosition)
    {
        Vector2i position;

        if (sizeX > 150 || sizeZ > 150)
        {
            return worldPositionCenter - new Vector2i((sizeX - 150) / 2, (sizeZ - 150) / 2);
        }

        try
        {
            position = new Vector2i(
                gameRandom.RandomRange(worldPosition.x + 10, worldPosition.x + 150 - sizeX - 10),
                gameRandom.RandomRange(worldPosition.y + 10, worldPosition.y + 150 - sizeZ - 10)
            );
        }
        catch
        {
            position = worldPositionCenter - new Vector2i(
                sizeX / 2,
                sizeZ / 2
            );
        }


        return position;
    }

    private static bool OverlapsPrefab(StreetTile tile, Vector2i position, int sizeX, int sizeZ)
    {
        int margin = 100;
        int halfWorldSize = WorldBuilder.Instance.WorldSize / 2;

        foreach (var st in tile.GetNeighbors8way())
        {
            if (!st.HasPrefabs)
                continue;

            foreach (var pdi in st.StreetTilePrefabDatas)
            {
                var p1 = new Vector3i(position.x - halfWorldSize, 0, position.y - halfWorldSize);
                var s1 = new Vector3i(sizeX, 0, sizeZ);

                if (CaveUtils.OverLaps2D(p1, s1, pdi.boundingBoxPosition, pdi.boundingBoxSize, margin))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool PositionIsValid(StreetTile st, Rect prefabRectangle, Vector2i prefabPosition, int maxSize, int sizeX, int sizeZ)
    {
        // if (OverlapsPrefab(st, prefabPosition, sizeX, sizeZ))
        // {
        //     return false;
        // }

        Rect rect2 = new Rect(
            prefabRectangle.min - new Vector2(maxSize, maxSize) / 2f,
            prefabRectangle.size + new Vector2(maxSize, maxSize)
        )
        {
            center = new Vector2(prefabPosition.x + sizeZ / 2, prefabPosition.y + sizeX / 2)
        };

        return !(
            rect2.max.x >= WorldBuilder.Instance.WorldSize
            || rect2.min.x < 0f
            || rect2.max.y >= WorldBuilder.Instance.WorldSize
            || rect2.min.y < 0f
        );
    }

    private static Vector2 GetEdgeCenter(int rotation, Vector2i prefabPosition, int sizeX, int sizeZ)
    {
        switch (rotation)
        {
            case 0:
                return new Vector2(prefabPosition.x + sizeX / 2, prefabPosition.y);

            case 1:
                return new Vector2(prefabPosition.x + sizeX, prefabPosition.y + sizeZ / 2);

            case 2:
                return new Vector2(prefabPosition.x + sizeX / 2, prefabPosition.y + sizeZ);

            case 3:
                return new Vector2(prefabPosition.x, prefabPosition.y + sizeZ / 2);

            default:
                throw new Exception($"[Cave] invalid rotation: '{rotation}'");
        }
    }

    private static List<Prefab.Marker> GetRotatedRoadExitMarkers(PrefabData wildernessPrefab, int rotation)
    {
        List<Prefab.Marker> markers = wildernessPrefab
            .RotatePOIMarkers(_bLeft: true, rotation)
            .Where(marker => marker.markerType == Prefab.Marker.MarkerTypes.RoadExit)
            .ToList();

        return markers;
    }

    private static void ProcessRoadExits(PrefabDataInstance pdi, StreetTile tile, Rect prefabRectangle)
    {
        if (pdi.prefab.POIMarkers == null)
            return;

        var prefabPosition = new Vector2i(pdi.boundingBoxPosition.x, pdi.boundingBoxPosition.z);
        var sizeX = pdi.boundingBoxSize.x;
        var sizeZ = pdi.boundingBoxSize.y;

        var roadRadius = 0f;
        var rotatedPosition = GetEdgeCenter(pdi.rotation, prefabPosition, sizeX, sizeZ);
        var roadMarkers = GetRotatedRoadExitMarkers(pdi.prefab, pdi.rotation);

        var vector3i2 = new Vector3i(
            tile.subHalfWorld(prefabPosition.x),
            tile.getHeightCeil(prefabRectangle.center),
            tile.subHalfWorld(prefabPosition.y)
        );

        if (roadMarkers.Count == 0)
            return;

        int index = gameRandom.RandomRange(0, roadMarkers.Count);
        Vector3i start = roadMarkers[index].Start;
        int sizeZX = (roadMarkers[index].Size.x > roadMarkers[index].Size.z) ? roadMarkers[index].Size.x : roadMarkers[index].Size.z;
        roadRadius = Mathf.Max(roadRadius, sizeZX / 2f);
        string groupName = roadMarkers[index].GroupName;

        Vector2 vector2 = new Vector2(
            start.x + roadMarkers[index].Size.x / 2f,
            start.z + roadMarkers[index].Size.z / 2f
        );

        rotatedPosition = new Vector2(
            prefabPosition.x + vector2.x,
            prefabPosition.y + vector2.y
        );

        Vector2 markerCenter = rotatedPosition;

        bool isPrefabPath = false;

        if (roadMarkers.Count > 1)
        {
            roadMarkers = pdi.prefab.POIMarkers.FindAll((Prefab.Marker m) => m.MarkerType == Prefab.Marker.MarkerTypes.RoadExit && m.Start != start && m.GroupName == groupName);
            if (roadMarkers.Count > 0)
            {
                index = gameRandom.RandomRange(0, roadMarkers.Count);
                markerCenter = new Vector2(
                    prefabPosition.x + roadMarkers[index].Start.x + roadMarkers[index].Size.x / 2f,
                    prefabPosition.y + roadMarkers[index].Start.z + roadMarkers[index].Size.z / 2f
                );
            }
            isPrefabPath = true;
        }

        Path path = new Path(_isCountryRoad: true, roadRadius);
        path.FinalPathPoints.Add(new Vector2(rotatedPosition.x, rotatedPosition.y));
        path.pathPoints3d.Add(new Vector3(rotatedPosition.x, vector3i2.y, rotatedPosition.y));
        path.FinalPathPoints.Add(new Vector2(markerCenter.x, markerCenter.y));
        path.pathPoints3d.Add(new Vector3(markerCenter.x, vector3i2.y, markerCenter.y));
        path.IsPrefabPath = isPrefabPath;
        path.StartPointID = pdi.id;
        path.EndPointID = pdi.id;

        WorldBuilder.Instance.wildernessPaths.Add(path);

        if (roadRadius != 0f)
        {
            WildernessPlanner.WildernessPathInfos.Add(
                new WorldBuilder.WildernessPathInfo(
                    new Vector2i(rotatedPosition),
                    pdi.id,
                    roadRadius,
                    WorldBuilder.Instance.GetBiome((int)rotatedPosition.x,
                    (int)rotatedPosition.y
                )
            ));
        }
    }

    private static void SetNeighborsUsed(Rect prefabRectangle)
    {
        var prefabMin_x = Mathf.FloorToInt(prefabRectangle.x) - 1;
        var prefabMax_x = Mathf.CeilToInt(prefabRectangle.xMax) + 1;
        var prefabMin_z = Mathf.FloorToInt(prefabRectangle.y) - 1;
        var prefabMax_z = Mathf.CeilToInt(prefabRectangle.yMax) + 1;

        for (int x = prefabMin_x; x < prefabMax_x; x += 150)
        {
            for (int z = prefabMin_z; z < prefabMax_z; z += 150)
            {
                StreetTile streetTileWorld = WorldBuilder.Instance.GetStreetTileWorld(x, z);

                if (streetTileWorld != null)
                {
                    streetTileWorld.Used = true;
                }
            }
        }
    }

    private static void SetPathBlocked(Rect prefabRectangle)
    {
        int prefabMin_x = Mathf.FloorToInt(prefabRectangle.x / 10f) - 1;
        int prefabMax_x = Mathf.CeilToInt(prefabRectangle.xMax / 10f) + 1;
        int prefabMin_y = Mathf.FloorToInt(prefabRectangle.y / 10f) - 1;
        int prefabMax_y = Mathf.CeilToInt(prefabRectangle.yMax / 10f) + 1;

        for (int x = prefabMin_x; x < prefabMax_x; x++)
        {
            for (int z = prefabMin_y; z < prefabMax_y; z++)
            {
                if (
                       x < 0 && x >= WorldBuilder.Instance.PathingGrid.GetLength(0)
                    || z < 0 && z >= WorldBuilder.Instance.PathingGrid.GetLength(1)
                )
                    continue;

                if (x == prefabMin_x || x == prefabMax_x - 1 || z == prefabMin_y || z == prefabMax_y - 1)
                {
                    PathingUtils.SetPathBlocked(x, z, 2);
                }
                else
                {
                    PathingUtils.SetPathBlocked(x, z, isBlocked: true);
                }

            }
        }
    }

    private static bool TrySpawnCaveEntrance(
        Rect prefabRectangle,
        Vector2i prefabPosition,
        int sizeX,
        int sizeZ,
        byte rotation,
        PrefabData wildernessPrefab,
        StreetTile tile
    )
    {
        Log.Out($"[Cave] rotation3: {rotation}");

        var medianHeight = Mathf.CeilToInt(WorldBuilder.Instance.GetHeight(
            (int)prefabRectangle.center.x,
            (int)prefabRectangle.center.y
        ));

        if (medianHeight + wildernessPrefab.yOffset < 2)
        {
            return false;
        }

        BiomeType biome = WorldBuilder.Instance.GetBiome(
            (int)prefabRectangle.center.x,
            (int)prefabRectangle.center.y
        );

        for (int x = prefabPosition.x; x < prefabPosition.x + sizeX; x++)
        {
            for (int z = prefabPosition.y; z < prefabPosition.y + sizeZ; z++)
            {
                if (
                       x >= WorldBuilder.Instance.WorldSize || x < 0
                    || z >= WorldBuilder.Instance.WorldSize || z < 0
                    || WorldBuilder.Instance.GetWater(x, z) > 0
                    || WorldBuilder.Instance.GetBiome(x, z) != biome
                    || Mathf.Abs(WorldBuilder.Instance.GetHeight(x, z) - medianHeight) > 11
                )
                {
                    return false;
                }
            }
        }

        var prefabId = PrefabManager.PrefabInstanceId++;
        var prefabWorldPos = new Vector3i(
            tile.subHalfWorld(prefabPosition.x),
            tile.getHeightCeil(prefabRectangle.center) + wildernessPrefab.yOffset + 1,
            tile.subHalfWorld(prefabPosition.y)
        );

        PrefabDataInstance pdi = new PrefabDataInstance(
            prefabId,
            new Vector3i(prefabWorldPos.x, medianHeight + wildernessPrefab.yOffset + 1, prefabWorldPos.z),
            rotation,
            wildernessPrefab
        );

        Log.Out($"[Cave] roation={rotation}, pdi.rotation={pdi.rotation}");

        ProcessRoadExits(pdi, tile, prefabRectangle);

        tile.SpawnMarkerPartsAndPrefabsWilderness(wildernessPrefab, new Vector3i(prefabPosition.x, Mathf.CeilToInt(medianHeight + wildernessPrefab.yOffset + 1), prefabPosition.y), (byte)rotation);
        tile.AddPrefab(pdi);
        WorldBuilder.Instance.WildernessPrefabCount++;

        if (medianHeight != tile.getHeightCeil(prefabRectangle.min.x, prefabRectangle.min.y) || medianHeight != tile.getHeightCeil(prefabRectangle.max.x, prefabRectangle.min.y) || medianHeight != tile.getHeightCeil(prefabRectangle.min.x, prefabRectangle.max.y) || medianHeight != tile.getHeightCeil(prefabRectangle.max.x, prefabRectangle.max.y))
        {
            tile.WildernessPOICenter = new Vector2i(prefabRectangle.center);
            tile.WildernessPOISize = Mathf.RoundToInt(Mathf.Max(prefabRectangle.size.x, prefabRectangle.size.y));
            tile.WildernessPOIHeight = medianHeight;
        }

        SetPathBlocked(prefabRectangle);
        SetNeighborsUsed(prefabRectangle);

        return true;
    }

    private static bool SpawnWildernessCaveEntrance(StreetTile tile, PrefabData wildernessPrefab)
    {
        Vector2i worldPositionCenter = tile.WorldPositionCenter;
        Vector2i worldPosition = tile.WorldPosition;

        int maxTries = 6;

        while (maxTries-- > 0)
        {
            int sizeX = wildernessPrefab.size.x;
            int sizeZ = wildernessPrefab.size.z;
            int maxSize = CaveUtils.FastMax(sizeX, sizeZ);
            int rotation = gameRandom.RandomRange(0, 4);

            Log.Out($"[Cave] rotation1: {rotation}");

            if (rotation == 1 || rotation == 3)
            {
                sizeX = wildernessPrefab.size.z;
                sizeZ = wildernessPrefab.size.x;
            }

            var prefabPosition = GetRandomPosition(sizeX, sizeZ, worldPositionCenter, worldPosition);
            var prefabRectangle = new Rect(prefabPosition.x, prefabPosition.y, maxSize, maxSize);

            if (!PositionIsValid(tile, prefabRectangle, prefabPosition, maxSize, sizeX, sizeZ))
            {
                // Log.Out($"[Cave] Invalid position to spawn '{wildernessPrefab.Name}'");
                continue;
            }

            Log.Out($"[Cave] rotation2: {rotation}");

            if (TrySpawnCaveEntrance(prefabRectangle, prefabPosition, sizeX, sizeZ, (byte)rotation, wildernessPrefab, tile))
            {
                return true;
            }

            Log.Out($"[Cave] spawning '{wildernessPrefab.Name}' failed.");
        }

        Log.Out($"[Cave] fail to spawn prefab '{wildernessPrefab.Name}', remaining tries: {maxTries}");
        return false;
    }

}