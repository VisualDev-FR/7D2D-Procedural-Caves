using System;
using System.Collections.Generic;
using UnityEngine;

public static class LegacyCaveSystem
{
    private static FastNoiseLite fastNoiseZX;

    private static FastNoiseLite fastNoiseY;

    private static BlockValue caveAir = new BlockValue((uint)Block.GetBlockByName("air").blockID);

    private static BlockValue pillarBlock = new BlockValue((uint)Block.GetBlockByName("terrDesertGround").blockID);

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

        fastNoiseZX = CaveConfig.GetFastNoiseZX();
        fastNoiseY = CaveConfig.GetFastNoiseY();

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

        fastNoiseZX = CaveConfig.GetFastNoiseZX();
        fastNoiseY = CaveConfig.GetFastNoiseY();

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

    private static void PlaceBlock(Chunk chunk, Vector3i position, bool isPillar = false)
    {
        // Make sure the position is in bounds, and not currently air. no sense in changing it
        if (position.x > 15 || position.z > 15)
            return;

        if (position.x < 0 || position.z < 0)
            return;

        if (chunk.GetBlock(position.x, position.y, position.z).isair)
            return;

        if (!isPillar)
        {
            chunk.SetBlockRaw(position.x, position.y, position.z, caveAir);
            chunk.SetDensity(position.x, position.y, position.z, MarchingCubes.DensityAir);
            return;
        }

        chunk.SetBlockRaw(position.x, position.y, position.z, pillarBlock);
        chunk.SetDensity(position.x, position.y, position.z, MarchingCubes.DensityTerrain);
    }

    // Changes the blocks. This works better when doing the cave openings, vs the prefab
    private static void PlaceAround(Chunk chunk, Vector3i position, bool isPillar = false)
    {
        PlaceBlock(chunk, position, isPillar);
        PlaceBlock(chunk, position + Vector3i.right, isPillar);
        PlaceBlock(chunk, position + Vector3i.left, isPillar);

        PlaceBlock(chunk, position + Vector3i.forward, isPillar);
        PlaceBlock(chunk, position + Vector3i.forward + Vector3i.right, isPillar);
        PlaceBlock(chunk, position + Vector3i.forward + Vector3i.left, isPillar);

        PlaceBlock(chunk, position + Vector3i.back, isPillar);
        PlaceBlock(chunk, position + Vector3i.back + Vector3i.right, isPillar);
        PlaceBlock(chunk, position + Vector3i.back + Vector3i.left, isPillar);
    }

    private static void Logging(string proc_name, string message)
    {
        AdvLogging.DisplayLog("LegacyTunneler", proc_name, message);
    }

}