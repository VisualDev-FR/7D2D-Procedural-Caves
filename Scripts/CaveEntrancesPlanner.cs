using System.Collections.Generic;
using WorldGenerationEngineFinal;
using UnityEngine;
using System.Linq;
using System;

// refactored WildernessPlanner, to allow more control on cave entrances placement

public static class CaveEntrancesPlanner
{
    private static GameRandom gameRandom;

    public static List<PrefabData> SpawnCaveEntrances()
    {
        gameRandom = GameRandomManager.Instance.CreateGameRandom(WorldBuilder.Instance.Seed + 4096953);

        var spawnedEntrances = new List<PrefabData>();
        var wildernessTiles = GetWildernessTiles();
        var tileIndex = 0;
        var maxRolls = 500;

        if (wildernessTiles.Count == 0)
        {
            Log.Error("[Cave] No wilderness streetTile was found");
            return spawnedEntrances;
        }

        while (tileIndex < wildernessTiles.Count && --maxRolls > 0)
        {
            var tile = wildernessTiles[tileIndex];
            var prefab = CavePlanner.SelectRandomWildernessEntrance();
            var succeed = SpawnWildernessCaveEntrance(tile, prefab);

            if (succeed)
            {
                Log.Out($"[Cave] Entrance spawned: '{prefab.Name}'");
                spawnedEntrances.Add(prefab);
                tileIndex++;
            }
            else
            {
                Log.Warning($"[Cave] fail to spawn entrance '{prefab.Name}'");
            }
        }

        return spawnedEntrances;
    }

    private static bool IsWildernessStreetTile(StreetTile st)
    {
        return
            !st.OverlapsRadiation
            && !st.AllIsWater
            && !st.Used
            && (st.District == null || st.District.name == "wilderness");
    }

    private static List<StreetTile> GetWildernessTiles()
    {
        var result =
            from StreetTile st in WorldBuilder.Instance.StreetTileMap
            where IsWildernessStreetTile(st)
            select st;

        return result.ToList();
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

    private static bool PositionIsValid(Rect prefabRectangle, Vector2i prefabPosition, int maxSize, int sizeX, int sizeZ)
    {
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

    private static Vector2 GetRotatedPrefabPosition(int rotation, Vector2i prefabPosition, int sizeX, int sizeZ)
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
                throw new Exception("[Cave] invalid rotation");
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

    private static void ProcessRoadExits(PrefabDataInstance pdi, StreetTile tile)
    {
        if (pdi.prefab.POIMarkers == null)
            return;

        var roadRadius = 0f;
        var rotatedPosition = GetRotatedPrefabPosition(rotation, prefabPosition, sizeX, sizeZ);
        var roadMarkers = GetRotatedRoadExitMarkers(pdi, rotation);
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

        Vector2 vector3 = rotatedPosition;

        bool isPrefabPath = false;

        if (roadMarkers.Count > 1)
        {
            roadMarkers = pdi.POIMarkers.FindAll((Prefab.Marker m) => m.MarkerType == Prefab.Marker.MarkerTypes.RoadExit && m.Start != start && m.GroupName == groupName);
            if (roadMarkers.Count > 0)
            {
                index = gameRandom.RandomRange(0, roadMarkers.Count);
                vector3 = new Vector2((float)(prefabPosition.x + roadMarkers[index].Start.x) + (float)roadMarkers[index].Size.x / 2f, (float)(prefabPosition.y + roadMarkers[index].Start.z) + (float)roadMarkers[index].Size.z / 2f);
            }
            isPrefabPath = true;
        }

        Path path = new Path(_isCountryRoad: true, roadRadius);
        path.FinalPathPoints.Add(new Vector2(rotatedPosition.x, rotatedPosition.y));
        path.pathPoints3d.Add(new Vector3(rotatedPosition.x, vector3i2.y, rotatedPosition.y));
        path.FinalPathPoints.Add(new Vector2(vector3.x, vector3.y));
        path.pathPoints3d.Add(new Vector3(vector3.x, vector3i2.y, vector3.y));
        path.IsPrefabPath = isPrefabPath;
        path.StartPointID = prefabId;
        path.EndPointID = prefabId;
        WorldBuilder.Instance.wildernessPaths.Add(path);

        if (roadRadius != 0f)
        {
            WildernessPlanner.WildernessPathInfos.Add(
                new WorldBuilder.WildernessPathInfo(
                    new Vector2i(rotatedPosition),
                    prefabId,
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

        for (int j = prefabMin_x; j < prefabMax_x; j++)
        {
            for (int k = prefabMin_y; k < prefabMax_y; k++)
            {
                if (j >= 0 && j < WorldBuilder.Instance.PathingGrid.GetLength(0) && k >= 0 && k < WorldBuilder.Instance.PathingGrid.GetLength(1))
                {
                    if (j == prefabMin_x || j == prefabMax_x - 1 || k == prefabMin_y || k == prefabMax_y - 1)
                    {
                        PathingUtils.SetPathBlocked(j, k, 2);
                    }
                    else
                    {
                        PathingUtils.SetPathBlocked(j, k, isBlocked: true);
                    }
                }
            }
        }
    }

    private static bool TrySpawnCaveEntrance(
        Rect prefabRectangle,
        Vector2i prefabPosition,
        int sizeX,
        int sizeZ,
        PrefabData wildernessPrefab,
        StreetTile tile
    )
    {
        BiomeType biome = WorldBuilder.Instance.GetBiome(
            (int)prefabRectangle.center.x,
            (int)prefabRectangle.center.y
        );

        var positionX = prefabPosition.x;
        var medianHeight = Mathf.CeilToInt(WorldBuilder.Instance.GetHeight(
            (int)prefabRectangle.center.x,
            (int)prefabRectangle.center.y
        ));

        var list = new List<int>();

        while (true)
        {
            if (positionX < prefabPosition.x + sizeX)
            {
                for (int positionZ = prefabPosition.y; positionZ < prefabPosition.y + sizeZ; positionZ++)
                {
                    if (positionX >= WorldBuilder.Instance.WorldSize || positionX < 0 || positionZ >= WorldBuilder.Instance.WorldSize || positionZ < 0 || WorldBuilder.Instance.GetWater(positionX, positionZ) > 0 || biome != WorldBuilder.Instance.GetBiome(positionX, positionZ) || Mathf.Abs(Mathf.CeilToInt(WorldBuilder.Instance.GetHeight(positionX, positionZ)) - medianHeight) > 11)
                    {
                        return false;
                    }
                    list.Add((int)WorldBuilder.Instance.GetHeight(positionX, positionZ));
                }
                positionX++;
                continue;
            }

            medianHeight = tile.getMedianHeight(list);
            if (medianHeight + wildernessPrefab.yOffset < 2)
            {
                break;
            }

            var prefabWorldPos = new Vector3i(
                tile.subHalfWorld(prefabPosition.x),
                tile.getHeightCeil(prefabRectangle.center) + wildernessPrefab.yOffset + 1,
                tile.subHalfWorld(prefabPosition.y)
            );

            gameRandom.SetSeed((prefabPosition.x << 13) + (prefabPosition.y << 17));

            var rotation = gameRandom.RandomRange(0, 4);
            var prefabId = PrefabManager.PrefabInstanceId++;

            PrefabDataInstance pdi = new PrefabDataInstance(
                prefabId,
                new Vector3i(prefabWorldPos.x, medianHeight + wildernessPrefab.yOffset + 1, prefabWorldPos.z),
                (byte)rotation,
                wildernessPrefab
            );

            ProcessRoadExits(pdi);

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

            GameRandomManager.Instance.FreeGameRandom(gameRandom);

            return true;
        }

        return false;
    }

    private static bool SpawnWildernessCaveEntrance(StreetTile tile, PrefabData wildernessPrefab)
    {
        Vector2i worldPositionCenter = tile.WorldPositionCenter;
        Vector2i worldPosition = tile.WorldPosition;

        int maxTries = 6;

        while (maxTries-- < 0)
        {
            int sizeX = wildernessPrefab.size.x;
            int sizeZ = wildernessPrefab.size.z;
            int maxSize = CaveUtils.FastMax(sizeX, sizeZ);
            int rotation = gameRandom.RandomRange(0, 4);

            if (rotation == 1 || rotation == 3)
            {
                sizeX = wildernessPrefab.size.z;
                sizeZ = wildernessPrefab.size.x;
            }

            var prefabPosition = GetRandomPosition(sizeX, sizeZ, worldPositionCenter, worldPosition);
            var prefabRectangle = new Rect(prefabPosition.x, prefabPosition.y, maxSize, maxSize);

            if (!PositionIsValid(prefabRectangle, prefabPosition, maxSize, sizeX, sizeZ))
            {
                continue;
            }

            if (TrySpawnCaveEntrance(prefabRectangle, prefabPosition, rotation, sizeX, sizeZ, wildernessPrefab, tile))
            {
                return true;
            }
        }

        GameRandomManager.Instance.FreeGameRandom(gameRandom);
        return false;
    }

}