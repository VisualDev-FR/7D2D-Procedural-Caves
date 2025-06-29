/*
    refactored WildernessPlanner, allowing more control on cave entrances placement
*/

using System.Collections.Generic;
using WorldGenerationEngineFinal;
using UnityEngine;
using System.Linq;
using System;


public class CaveEntrancesPlanner
{
    private GameRandom gameRandom;

    private WorldBuilder WorldBuilder => cavePrefabManager.worldBuilder;

    private readonly CavePrefabManager cavePrefabManager;

    public CaveEntrancesPlanner(CavePrefabManager cavePrefabManager)
    {
        this.cavePrefabManager = cavePrefabManager;
    }

    public void SpawnNaturalEntrances()
    {
        gameRandom = GameRandomManager.Instance.CreateGameRandom(WorldBuilder.Seed);

        var minDepth = 20;

        foreach (var tile in GetShuffledWildernessTiles())
        {
            if (tile.Used) continue;

            var center = tile.WorldPositionCenter;
            center.x += gameRandom.Next(-20, 20);
            center.y += gameRandom.Next(-20, 20);

            var terrainHeight = tile.getHeightCeil(center.x, center.y);

            if (terrainHeight < minDepth) continue;

            var entranceY = gameRandom.Next(CaveConfig.bedRockMargin, terrainHeight - minDepth);
            var entrancePosition = new Vector3i(center.x, entranceY, center.y);

            if (WorldBuilder.GetWater(center.x, center.y) == 0)
            {
                cavePrefabManager.AddNaturalEntrance(entrancePosition);
            }
        }
    }

    private bool IsWildernessStreetTile(StreetTile st)
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

    private List<StreetTile> GetShuffledWildernessTiles()
    {
        var result =
            from StreetTile st in WorldBuilder.StreetTileMap
            where IsWildernessStreetTile(st)
            select st;

        return result
            .OrderBy(tile => gameRandom.Next())
            .ToList();
    }

}