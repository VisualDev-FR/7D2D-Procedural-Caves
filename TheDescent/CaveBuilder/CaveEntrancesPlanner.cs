/*
    refactored WildernessPlanner, allowing more control on cave entrances placement
*/

using System.Collections.Generic;
using WorldGenerationEngineFinal;
using System.Linq;


public class CaveEntrancesPlanner
{
    private GameRandom gameRandom;

    private readonly CavePrefabManager cavePrefabManager;

    public CaveEntrancesPlanner(CavePrefabManager cavePrefabManager)
    {
        this.cavePrefabManager = cavePrefabManager;
    }

    public void SpawnNaturalEntrances(WorldBuilder worldBuilder)
    {
        gameRandom = GameRandomManager.Instance.CreateGameRandom(worldBuilder.Seed);

        var minDepth = 20;

        foreach (var tile in GetShuffledWildernessTiles(worldBuilder))
        {
            if (tile.Used) continue;

            var center = tile.WorldPositionCenter;
            center.x += gameRandom.Next(-20, 20);
            center.y += gameRandom.Next(-20, 20);

            var terrainHeight = tile.getHeightCeil(center.x, center.y);

            if (terrainHeight < minDepth) continue;

            var entranceY = gameRandom.Next(CaveConfig.bedRockMargin, terrainHeight - minDepth);
            var entrancePosition = new Vector3i(center.x, entranceY, center.y);

            if (worldBuilder.data.GetWater(center.x, center.y) == 0)
            {
                cavePrefabManager.AddNaturalEntrance(entrancePosition);
            }
        }
    }

    public void SpawnNaturalEntrances(WorldDatas worldDatas)
    {
        gameRandom = GameRandomManager.Instance.CreateGameRandom(worldDatas.seed);

        var minDepth = 20;

        foreach (var tile in worldDatas.GetStreetTiles())
        {
            if (tile.HasPrefabs) continue;

            var center = tile.worldPositionCenter;
            center.x += gameRandom.Next(-20, 20);
            center.y += gameRandom.Next(-20, 20);

            var terrainHeight = worldDatas.GetHeightCeil(center.x, center.y);

            if (terrainHeight < minDepth) continue;

            var entranceY = gameRandom.Next(CaveConfig.bedRockMargin, terrainHeight - minDepth);
            var entrancePosition = new Vector3i(center.x, entranceY, center.y);

            if (!worldDatas.IsWater(center.x, center.y))
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

    private List<StreetTile> GetShuffledWildernessTiles(WorldBuilder worldBuilder)
    {
        var result =
            from StreetTile st in worldBuilder.StreetTileMap
            where IsWildernessStreetTile(st)
            select st;

        return result
            .OrderBy(tile => gameRandom.Next())
            .ToList();
    }

}