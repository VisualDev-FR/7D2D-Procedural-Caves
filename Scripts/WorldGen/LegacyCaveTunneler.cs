using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using HarmonyLib;
using UnityEngine;
public static class LegacyCaveSystem
{
    private static BlockValue caveAir = new BlockValue((uint)Block.GetBlockByName("air").blockID);

    private static BlockValue bottomCaveDecoration = new BlockValue((uint)Block.GetBlockByName("cntCaveFloorRandomLootHelper").blockID);

    private static BlockValue topCaveDecoration = new BlockValue((uint)Block.GetBlockByName("cntCaveCeilingRandomLootHelper").blockID);

    // private static GameRandom random => GameManager.Instance.World.GetGameRandom();

    public static void Add2DCaveToChunk(Chunk chunk)
    {
        if (chunk == null)
        {
            Log.Out($"Null chunk {chunk.ChunkPos}");
            return;
        }

        BlockValue caveBlock = CaveConfig.isSolid
        ? new BlockValue((uint)Block.GetBlockByName("concreteShapes:cube").blockID)
        : caveAir;

        var fastNoiseZX = CaveConfig.GetFastNoiseZX();
        var chunkPos = chunk.GetWorldPos();

        for (var chunkX = 0; chunkX < 16; chunkX++)
        {
            for (var chunkZ = 0; chunkZ < 16; chunkZ++)
            {
                var worldX = chunkPos.x + chunkX;
                var worldZ = chunkPos.z + chunkZ;

                var terrainHeight = chunk.GetTerrainHeight(chunkX, chunkZ) - 10;

                float noise = (1 + fastNoiseZX.GetNoise(worldX, worldZ)) / 2;

                if (CaveConfig.invert)
                    noise = 1 - noise;

                if (noise < 0 || noise > 1)
                    Log.Error($"noise = {noise}");

                if (noise > CaveConfig.NoiseThreeshold)
                    continue;

                for (int chunkY = CaveConfig.cavePos2D; chunkY < CaveConfig.cavePos2D + CaveConfig.caveHeight2D; chunkY++)
                {
                    chunk.SetBlockRaw(chunkX, chunkY, chunkZ, caveBlock);
                    chunk.SetDensity(chunkX, chunkY, chunkZ, MarchingCubes.DensityAir);
                }
            }
        }

        // AddDecorationsToCave(chunk);
    }

    public static void Add3DCaveToChunk(Chunk chunk)
    {
        if (chunk == null)
        {
            Log.Out($"Null chunk {chunk.ChunkPos}");
            return;
        }

        BlockValue caveBlock = CaveConfig.isSolid
        ? new BlockValue((uint)Block.GetBlockByName("concreteShapes:cube").blockID)
        : caveAir;

        var fastNoiseZX = CaveConfig.GetFastNoiseZX();
        var fastNoiseY = CaveConfig.GetFastNoiseY();

        var chunkPos = chunk.GetWorldPos();

        for (var chunkX = 0; chunkX < 16; chunkX++)
        {
            for (var chunkZ = 0; chunkZ < 16; chunkZ++)
            {
                var worldX = chunkPos.x + chunkX;
                var worldZ = chunkPos.z + chunkZ;

                float noiseZX = 0.5f * (1 + fastNoiseZX.GetNoise(worldX, worldZ));

                if (CaveConfig.invert)
                    noiseZX = 1 - noiseZX;

                if (noiseZX < 0 || noiseZX > 1)
                    Log.Error($"noise = {noiseZX}");

                if (noiseZX > CaveConfig.NoiseThreeshold)
                    continue;

                float noiseY = 0.5f * (1 + fastNoiseY.GetNoise(worldX, worldZ));

                int terrainHeight = chunk.GetTerrainHeight(chunkX, chunkZ) - 10;
                int caveBottom = (int)(1.5 * noiseY * terrainHeight);
                int caveTop = caveBottom + CaveConfig.caveHeight2D;

                for (int chunkY = caveBottom; chunkY < caveTop; chunkY++)
                {

                    if (chunkY > terrainHeight + 10)
                    {
                        Log.Out($"Cave entrance at [{chunkX}, {chunkY}, {chunkZ}]");
                        break;
                    }

                    chunk.SetBlockRaw(chunkX, chunkY, chunkZ, caveBlock);
                    chunk.SetDensity(chunkX, chunkY, chunkZ, MarchingCubes.DensityAir);
                }
            }
        }
    }

    public static void AddDecorationsToCave(Chunk chunk)
    {
        if (chunk == null)
            return;

        var chunkPos = chunk.GetWorldPos();

        GameRandom random = Utils.RandomFromSeedOnPos(chunk.ChunkPos.x, chunk.ChunkPos.z, GameManager.Instance.World.Seed);

        // PlaceCaveEntrance(chunk);
        GeneratePrefabs(chunk, random);

        // Decorate decorate the cave spots with blocks. Shrink the chunk loop by 1 on its edges so we can safely check surrounding blocks.
        for (var chunkX = 1; chunkX < 15; chunkX++)
        {
            for (var chunkZ = 1; chunkZ < 15; chunkZ++)
            {
                var worldX = chunkPos.x + chunkX;
                var worldZ = chunkPos.z + chunkZ;

                var tHeight = chunk.GetTerrainHeight(chunkX, chunkZ);

                // One test world, we blew through a threshold.
                if (tHeight > 250)
                    tHeight = 240;

                // Move from the bottom up, leaving the last few blocks untouched.
                //for (int y = tHeight; y > 5; y--)
                for (var chunkY = 5; chunkY < tHeight - 2; chunkY++)
                {
                    var b = chunk.GetBlock(chunkX, chunkY, chunkZ);

                    if (b.type != caveAir.type)
                        continue;

                    var under = chunk.GetBlock(chunkX, chunkY - 1, chunkZ);
                    var above = chunk.GetBlock(chunkX, chunkY + 1, chunkZ);

                    BlockValue bottomBlock;
                    // Check the floor for possible decoration
                    if (under.Block.shape.IsTerrain())
                    {
                        bottomBlock = BlockPlaceholderMap.Instance.Replace(bottomCaveDecoration, random, worldX, worldZ);

                        // // Place alternative blocks down deeper
                        // if (chunkY < CaveConfig.deepCaveThreshold)
                        //     bottomBlock = BlockPlaceholderMap.Instance.Replace(bottomDeepCaveDecoration, random, worldX, worldZ);

                        chunk.SetBlock(GameManager.Instance.World, chunkX, chunkY, chunkZ, bottomBlock);
                        continue;
                    }

                    // Check the ceiling to see if its a ceiling decoration
                    if (!above.Block.shape.IsTerrain())
                        continue;

                    bottomBlock = BlockPlaceholderMap.Instance.Replace(topCaveDecoration, random, worldX, worldZ);
                    chunk.SetBlock(GameManager.Instance.World, chunkX, chunkY, chunkZ, bottomBlock);
                }
            }
        }
    }

    private static string SelectRandomPOI(GameRandom random)
    {
        return CaveConfig.CavePOIs[random.RandomRange(0, CaveConfig.CavePOIs.Length)];
    }

    private static void GeneratePrefabs(Chunk chunk, GameRandom random)
    {
        string caveAirName = caveAir.Block.GetBlockName();

        // Random chance to place a prefab to try to sparse them out.
        if (random.RandomRange(0, 10) > 3)
            return;

        Vector3i prefabWorldPosition = Vector3i.zero;
        Vector3i prefabChunkPos = Vector3i.zero;

        int chunkX = GameManager.Instance.World.GetGameRandom().RandomRange(0, 16);
        int chunkZ = GameManager.Instance.World.GetGameRandom().RandomRange(0, 16);

        for (int chunkY = 0; chunkY < chunk.GetHeight(chunkX, chunkZ); chunkY++)
        {
            BlockValue upperBlock = chunk.GetBlock(chunkX, chunkY + 1, chunkZ);

            if (upperBlock.Block.GetBlockName() == caveAirName)
            {
                prefabChunkPos = new Vector3i(chunkX, chunkY, chunkZ);
                prefabWorldPosition = chunk.ToWorldPos(prefabChunkPos);
                break;
            }
        }

        // Decide what kind of prefab to spawn in.
        string poiName = SelectRandomPOI(random);
        Prefab prefab = FindOrCreatePrefab(poiName);

        if (prefab == null || prefabWorldPosition == Vector3i.zero)
            return;

        try
        {
            // PrefabInstance.CopyIntoChunk
            var prefabTags = prefab.Tags;
            prefab.CopyBlocksIntoChunkNoEntities(GameManager.Instance.World, chunk, prefabWorldPosition, true);
            bool bSpawnEnemies = GameManager.Instance.World.IsEditor() || GameStats.GetBool(EnumGameStats.IsSpawnEnemies);
            var entityInstanceIds = new List<int>();
            prefab.CopyEntitiesIntoChunkStub(chunk, prefabWorldPosition, entityInstanceIds, true);
        }
        catch (Exception ex)
        {
            Debug.Log("Warning: Could not copy over prefab: " + poiName + " " + ex);
        }
    }

    private static List<Chunk> GetChunkNeighbors(Chunk chunk)
    {
        Vector3i chunkPos = chunk.ToWorldPos();
        World world = GameManager.Instance.World;

        Vector3i[] positions = new Vector3i[4]{
            new Vector3i(chunkPos.x + 16, chunkPos.y, chunkPos.z),
            new Vector3i(chunkPos.x - 1, chunkPos.y, chunkPos.z),
            new Vector3i(chunkPos.x, chunkPos.y, chunkPos.z + 16),
            new Vector3i(chunkPos.x, chunkPos.y, chunkPos.z - 1),
        };

        // Log.Out($"[Caves] chunkID={chunk.GetHashCode()}");
        // Log.Out($"[Caves] chunkPos={chunk.ChunkPos}");

        var neighbors = new List<Chunk>();

        for (int i = 0; i < 4; i++)
        {
            if (world.GetChunkFromWorldPos(positions[i]) is Chunk neighbor)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    private static void printChunkNeighbors(Chunk chunk)
    {
        Vector3i chunkPos = chunk.ToWorldPos();
        World world = GameManager.Instance.World;

        Vector3i[] neighbors = new Vector3i[4]{
            new Vector3i(chunkPos.x + 16, chunkPos.y, chunkPos.z),
            new Vector3i(chunkPos.x - 1, chunkPos.y, chunkPos.z),
            new Vector3i(chunkPos.x, chunkPos.y, chunkPos.z + 16),
            new Vector3i(chunkPos.x, chunkPos.y, chunkPos.z - 1),
        };

        Log.Out($"[Caves] chunkID={chunk.GetHashCode()}");
        Log.Out($"[Caves] chunkPos={chunk.ChunkPos}");

        for (int i = 0; i < 4; i++)
        {
            if (!(world.GetChunkFromWorldPos(neighbors[i]) is Chunk neighbor))
            {
                Log.Out($"[Caves] neighbors[{i}]=null");
            }
            else
            {
                Log.Out($"[Caves] neighbors[{i}]={neighbor.GetHashCode()}");
            }
        }
    }

    public static Prefab FindOrCreatePrefab(string strPOIname)
    {
        // Check if the prefab already exists.
        Prefab prefab = GameManager.Instance.GetDynamicPrefabDecorator().GetPrefab(strPOIname, true, true, true);
        if (prefab != null)
            return prefab;

        // If it's not in the prefab decorator, load it up.
        prefab = new Prefab();
        prefab.Load(strPOIname, true, true, true);
        var location = PathAbstractions.PrefabsSearchPaths.GetLocation(strPOIname);
        prefab.LoadXMLData(location);

        if (string.IsNullOrEmpty(prefab.PrefabName))
            // prefab.PrefabName = strPOIname;
            return null;

        return prefab;
    }

}