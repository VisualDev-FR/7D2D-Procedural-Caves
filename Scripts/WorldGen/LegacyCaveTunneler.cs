using System;
using System.Collections.Generic;
using UnityEngine;
public static class LegacyCaveSystem
{
    private static BlockValue caveAir = new BlockValue((uint)Block.GetBlockByName("air").blockID);

    private static BlockValue bottomCaveDecoration = new BlockValue((uint)Block.GetBlockByName("concreteShapes:cube").blockID);

    private static BlockValue topCaveDecoration = new BlockValue((uint)Block.GetBlockByName("concreteShapes:cube").blockID);

    private static GameRandom random => GameManager.Instance.World.GetGameRandom();

    public static void AddCaveToChunk(Chunk chunk)
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

        // PlaceCaveEntrance(chunk);
        GeneratePrefabs(chunk);

        /* // Decorate decorate the cave spots with blocks. Shrink the chunk loop by 1 on its edges so we can safely check surrounding blocks.
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
        } */

    }

    private static string SelectRandomPOI(int caveDeep, int deepCaveThreshold)
    {
        if (caveDeep < deepCaveThreshold)
        {
            return CaveConfig.DeepCavePrefabs[random.RandomRange(0, CaveConfig.DeepCavePrefabs.Length)];
        }
        else
        {
            return CaveConfig.CavePOIs[random.RandomRange(0, CaveConfig.CavePOIs.Length)];
        }
    }

    private static void GeneratePrefabs(Chunk chunk)
    {
        var random = GameManager.Instance.World.GetGameRandom();

        // Random chance to place a prefab to try to sparse them out.
        if (random.RandomRange(0, 10) > 3)
            return;

        // Grab a random range slightly smaller than the chunk. This is to help pad them away from each other.
        var x = GameManager.Instance.World.GetGameRandom().RandomRange(0, 16);
        var z = GameManager.Instance.World.GetGameRandom().RandomRange(0, 16);

        var height = (int)chunk.GetHeight(x, z);
        if (height < 20)
            return;

        var maxHeight = height - 30;
        if (maxHeight < 1)
            maxHeight = 20;

        var y = random.RandomRange(0, maxHeight);

        if (y < 10)
            y = 5;

        var deepCaveThreshold = CaveConfig.deepCaveThreshold;
        var prefabDestination = Vector3i.zero;

        for (var checkLocation = 0; checkLocation < 10; checkLocation++)
        {
            var checkX = GameManager.Instance.World.GetGameRandom().RandomRange(0, 16);
            var checkZ = GameManager.Instance.World.GetGameRandom().RandomRange(0, 16);

            if (maxHeight <= deepCaveThreshold)
                continue;

            var checkY = GameManager.Instance.World.GetGameRandom().RandomRange(deepCaveThreshold, maxHeight);
            if (y < deepCaveThreshold)
                checkY = GameManager.Instance.World.GetGameRandom().RandomRange(2, deepCaveThreshold);

            var b = chunk.GetBlock(checkX, checkY, checkZ);

            if (!b.isair) continue;

            prefabDestination = chunk.ToWorldPos(new Vector3i(checkX, checkY, checkZ));
            y = checkY;
            break;
        }

        // Decide what kind of prefab to spawn in.
        string strPOI = SelectRandomPOI(y, deepCaveThreshold);

        var newPrefab = FindOrCreatePrefab(strPOI);
        if (newPrefab == null)
        {
            return;
        }

        if (prefabDestination == Vector3i.zero)
        {
            return;
        }

        var prefab = newPrefab.Clone();
        prefab.RotateY(true, random.RandomRange(4));

        try
        {
            // Winter Project counter-sinks all prefabs -8 into the ground. However, for underground spawning, we want to avoid this, as they are already deep enough
            // Instead, temporarily replace the tag with a custom one, so that the Harmony patch for the CopyIntoLocal of the winter project won't execute.
            var temp = prefab.Tags;
            prefab.Tags = POITags.Parse("SKIP_HARMONY_COPY_INTO_LOCAL");
            prefab.yOffset = 0;
            prefab.CopyBlocksIntoChunkNoEntities(GameManager.Instance.World, chunk, prefabDestination,
                true);
            var entityInstanceIds = new List<int>();
            prefab.CopyEntitiesIntoChunkStub(chunk, prefabDestination, entityInstanceIds, true);

            // Trying to track a crash in something.
            //prefab.CopyIntoLocal(GameManager.Instance.World.ChunkClusters[0], destination, true, true);
            // Restore any of the tags that might have existed before.
            prefab.Tags = temp;
            //  prefab.SnapTerrainToArea(GameManager.Instance.World.ChunkClusters[0], destination);
        }
        catch (Exception ex)
        {
            Debug.Log("Warning: Could not copy over prefab: " + strPOI + " " + ex);
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
            prefab.PrefabName = strPOIname;

        return prefab;
    }

}