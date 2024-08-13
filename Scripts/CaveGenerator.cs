using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CaveGenerator
{
    public static CaveBlocksProvider caveBlocksProvider;

    private static BlockValue caveAir = new BlockValue((uint)Block.GetBlockByName("air").blockID);

    private static BlockValue concreteBlock = new BlockValue((uint)Block.GetBlockByName("concreteShapes:cube").blockID);

    private static BlockValue bottomCaveDecoration = new BlockValue((uint)Block.GetBlockByName("cntCaveFloorRandomLootHelper").blockID);

    private static BlockValue topCaveDecoration = new BlockValue((uint)Block.GetBlockByName("cntCaveCeilingRandomLootHelper").blockID);

    public static void Init(string worldName)
    {
        caveBlocksProvider = new CaveBlocksProvider(worldName);
    }

    public static void GenerateCave(Chunk chunk)
    {
        if (chunk == null)
        {
            Log.Warning($"[Cave] Null chunk at {chunk.ChunkPos}");
            return;
        }


        GameRandom random = Utils.RandomFromSeedOnPos(chunk.ChunkPos.x, chunk.ChunkPos.z, GameManager.Instance.World.Seed);
        Vector3i chunkWorldPos = chunk.GetWorldPos();

        HashSet<CaveBlock> caveBlocks = caveBlocksProvider.GetCaveBlocks(chunk);

        if (caveBlocks == null)
            return;

        HashSet<Vector3> positions = caveBlocks.Select(block => block.BlockPos.ToVector3()).ToHashSet();

        foreach (CaveBlock caveBlock in caveBlocks)
        {
            var blockPos = caveBlock.BlockPos;

            try
            {
                chunk.SetBlockRaw(blockPos.x, blockPos.y, blockPos.z, caveAir);
                chunk.SetDensity(blockPos.x, blockPos.y, blockPos.z, MarchingCubes.DensityAir);
            }
            catch (Exception e)
            {
                Log.Error($"[Cave] {e.GetType()} (Chunk={caveBlock.ChunkPos}, block={caveBlock.BlockPos})");
            }

            var under = blockPos.ToVector3() + new Vector3(0, -1, 0);
            var above = blockPos.ToVector3() + new Vector3(0, +1, 0);

            bool isFloor = !positions.Contains(under);
            bool isCeiling = !positions.Contains(above);

            var worldX = chunkWorldPos.x + blockPos.x;
            var worldZ = chunkWorldPos.z + blockPos.z;

            if (isFloor)
            {
                BlockValue blockValue = BlockPlaceholderMap.Instance.Replace(bottomCaveDecoration, random, worldX, worldZ);
                chunk.SetBlockRaw(blockPos.x, blockPos.y, blockPos.z, blockValue);
            }
            else if (isCeiling)
            {
                BlockValue blockValue = BlockPlaceholderMap.Instance.Replace(topCaveDecoration, random, worldX, worldZ);
                chunk.SetBlockRaw(blockPos.x, blockPos.y, blockPos.z, blockValue);
            }
        }

        HashSet<CaveBlock> waterBlocks = caveBlocks.Where(block => block.isWater).ToHashSet();
        foreach (CaveBlock waterBlock in waterBlocks)
        {
            var blockPos = waterBlock.BlockPos;

            try
            {
                chunk.SetWaterRaw(blockPos.x, blockPos.y, blockPos.z, WaterValue.Full);
                // chunk.SetBlockRaw(blockPos.x, blockPos.y, blockPos.z, concreteBlock);
            }
            catch (Exception e)
            {
                Log.Error($"[Cave] {e.GetType()} (Chunk={waterBlock.ChunkPos}, block={waterBlock.BlockPos})");
            }
        }

    }

}