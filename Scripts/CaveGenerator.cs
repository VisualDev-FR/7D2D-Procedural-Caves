using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class CaveGenerator
{
    public static CaveBlocksProvider caveBlocksProvider;

    public static bool isEnabled = false;

    private static BlockValue caveAir = new BlockValue((uint)Block.GetBlockByName("caveAir").blockID);

    private static BlockValue concreteBlock = new BlockValue((uint)Block.GetBlockByName("concreteShapes:cube").blockID);

    private static BlockValue cntCaveFloor = new BlockValue((uint)Block.GetBlockByName("cntCaveFloor").blockID);

    private static BlockValue cntCaveCeiling = new BlockValue((uint)Block.GetBlockByName("cntCaveCeiling").blockID);

    public static void Init(string worldName)
    {
        isEnabled = Directory.Exists($"{GameIO.GetWorldDir(worldName)}/cavemap");

        if (isEnabled)
        {
            caveBlocksProvider = new CaveBlocksProvider(worldName);
        }
        else
        {
            Log.Warning($"[Cave] no cavemap found for world '{worldName}'");
        }
    }

    private static bool CanDecorateFlatCave(BlockValue _blockValue, Vector3i _blockPos)
    {
        var block = _blockValue.Block;

        if (!block.isMultiBlock)
        {
            foreach (var below in CaveUtils.offsetsBelow)
            {
                Vector3i position = _blockPos + below;

                if (caveBlocksProvider.IsCave(position))
                {
                    return false;
                }
            }

            return true;
        }

        return false;

        // Bounds bounds = block.multiBlockPos.CalcBounds(_blockValue.type, _blockValue.rotation);
        // bounds.center += _blockPos.ToVector3();

        // return true;
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

        HashSet<Vector3> positions = caveBlocks.Select(block => block.blockChunkPos.ToVector3()).ToHashSet();

        foreach (CaveBlock caveBlock in caveBlocks)
        {
            Vector3bf blockChunkPos = caveBlock.blockChunkPos;

            try
            {
                chunk.SetBlockRaw(blockChunkPos.x, blockChunkPos.y, blockChunkPos.z, caveAir);
                chunk.SetDensity(blockChunkPos.x, blockChunkPos.y, blockChunkPos.z, caveBlock.density);
            }
            catch (Exception e)
            {
                Log.Error($"[Cave] {e.GetType()} (Chunk={caveBlock.chunkPos}, block={caveBlock.blockChunkPos})");
            }

            var under = blockChunkPos.ToVector3() + new Vector3(0, -1, 0);
            var above = blockChunkPos.ToVector3() + new Vector3(0, +1, 0);

            bool isFloor = !positions.Contains(under);
            bool isCeiling = !positions.Contains(above);

            var worldX = chunkWorldPos.x + blockChunkPos.x;
            var worldZ = chunkWorldPos.z + blockChunkPos.z;

            BlockValue blockValue = concreteBlock;

            if (isFloor)
            {
                blockValue = BlockPlaceholderMap.Instance.Replace(cntCaveFloor, random, worldX, worldZ);
            }
            else if (isCeiling)
            {
                blockValue = BlockPlaceholderMap.Instance.Replace(cntCaveCeiling, random, worldX, worldZ);
            }
            else
            {
                continue;
            }

            if (CanDecorateFlatCave(blockValue, caveBlock.ToWorldPos()))
            {
                chunk.SetBlock(GameManager.Instance.World, blockChunkPos.x, blockChunkPos.y, blockChunkPos.z, blockValue);
            }
        }

        HashSet<CaveBlock> waterBlocks = caveBlocks.Where(block => block.isWater).ToHashSet();
        foreach (CaveBlock waterBlock in waterBlocks)
        {
            var blockPos = waterBlock.blockChunkPos;

            try
            {
                chunk.SetWaterRaw(blockPos.x, blockPos.y, blockPos.z, WaterValue.Full);
                // chunk.SetBlockRaw(blockPos.x, blockPos.y, blockPos.z, concreteBlock);
            }
            catch (Exception e)
            {
                Log.Error($"[Cave] {e.GetType()} (Chunk={waterBlock.chunkPos}, block={waterBlock.blockChunkPos})");
            }
        }

    }

}