using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

public static class CaveGenerator
{
    public static CaveBlocksProvider caveBlocksProvider;

    public static bool isEnabled = false;

    public static int caveAirType = caveAir.type;

    public static BlockValue caveAir = new BlockValue((uint)Block.GetBlockByName("caveAir").blockID);

    private static BlockValue concreteBlock = new BlockValue((uint)Block.GetBlockByName("concreteShapes:cube").blockID);

    private static BlockValue cntCaveFloor = new BlockValue((uint)Block.GetBlockByName("cntCaveFloor").blockID);

    private static BlockValue cntCaveFloorFlat = new BlockValue((uint)Block.GetBlockByName("cntCaveFloorFlat").blockID);

    private static BlockValue cntCaveCeiling = new BlockValue((uint)Block.GetBlockByName("cntCaveCeiling").blockID);

    private static BlockValue terrGravel = new BlockValue((uint)Block.GetBlockByName("terrGravel").blockID);

    public static void Init(string worldName)
    {
        isEnabled = Directory.Exists($"{GameIO.GetWorldDir(worldName)}/cavemap");

        if (isEnabled)
        {
            CaveBuilder.worldSize = GetWorldSize(worldName);
            caveBlocksProvider = new CaveBlocksProvider(worldName);

            Log.Out($"[Cave] init caveGenerator for world '{worldName}', size: {CaveBuilder.worldSize}");
        }
        else
        {
            Log.Warning($"[Cave] no cavemap found for world '{worldName}'");
        }
    }

    private static int GetWorldSize(string worldName)
    {
        string path = $"{GameIO.GetWorldDir(worldName)}/map_info.xml";

        var xmlDoc = new XmlDocument();
        xmlDoc.Load(path);

        var node = xmlDoc.SelectSingleNode("//property[@name='HeightMapSize']");

        if (node != null)
        {
            string heightMapSize = node.Attributes["value"].Value;

            return int.Parse(heightMapSize.Split(',')[0]);
        }
        else
        {
            throw new Exception("World Size not found!");
        }
    }

    private static bool IsFlatFloor(Vector3i cavePos)
    {
        int x0 = cavePos.x - 1;
        int z0 = cavePos.z - 1;
        int x1 = cavePos.x + 1;
        int z1 = cavePos.z + 1;
        int y = cavePos.y - 1;

        for (int x = x0; x < x1; x++)
        {
            for (int z = z0; z < z1; z++)
            {
                if (caveBlocksProvider.IsCave(x, y, z))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool CanPlaceDecoration(BlockValue blockValue, Vector3i worldPos)
    {
        var size = Vector3i.one;

        if (blockValue.Block.isMultiBlock)
        {
            size = blockValue.Block.multiBlockPos.dim - Vector3i.one;
        }

        if (blockValue.Block.BigDecorationRadius > 0)
        {
            size = new Vector3i(
                blockValue.Block.BigDecorationRadius,
                1,
                blockValue.Block.BigDecorationRadius
            );
        }

        int x0 = worldPos.x - 1;
        int z0 = worldPos.z - 1;
        int x1 = worldPos.x + size.x + 1;
        int z1 = worldPos.z + size.z + 1;
        int y = worldPos.y - 1;

        for (int x = x0; x < x1; x++)
        {
            for (int z = z0; z < z1; z++)
            {
                if (caveBlocksProvider.IsCave(x, y, z))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static BlockValue SpawnDecoration(HashSet<Vector3i> chunkBlocks, CaveBlock caveBlock, GameRandom random, int worldX, int worldY, int worldZ)
    {
        random.InternalSetSeed(worldX * 13 + worldZ);

        Vector3i worldPos = new Vector3i(worldX, worldY, worldZ);
        Vector3i blockPos = caveBlock.ToVector3i();
        Vector3i lower = blockPos + Vector3i.down;
        Vector3i upper = blockPos + Vector3i.up;

        bool isFloor = !chunkBlocks.Contains(lower) && chunkBlocks.Contains(upper);
        bool isCeiling = chunkBlocks.Contains(lower) && !chunkBlocks.Contains(upper);
        bool isFlatFloor = IsFlatFloor(worldPos);
        bool isWater = caveBlock.isWater;

        BlockValue placeHolder;

        if (isWater)
            placeHolder = cntCaveFloor;

        else if (isFlatFloor)
            placeHolder = cntCaveFloorFlat;

        else if (isFloor)
            placeHolder = cntCaveFloor;

        else if (isCeiling)
            placeHolder = cntCaveCeiling;

        else
            return caveAir;

        int maxTries = 20;

        while (maxTries-- > 0)
        {
            var blockValue = BlockPlaceholderMap.Instance.Replace(placeHolder, random, worldX, worldZ);

            if (!isFlatFloor)
            {
                return blockValue;
            }
            else if (blockValue.type == caveAir.type)
            {
                return BlockPlaceholderMap.Instance.Replace(cntCaveFloor, random, worldX, worldZ);
            }
            else if (CanPlaceDecoration(blockValue, worldPos))
            {
                return blockValue;
            }
        }

        return caveAir;
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

        HashSet<Vector3i> positions = caveBlocks?
            .Select(block => block.ToVector3i())
            .ToHashSet();

        foreach (CaveBlock caveBlock in caveBlocks)
        {
            Vector3bf blockChunkPos = caveBlock.blockChunkPos;

            try
            {
                chunk.SetBlockRaw(blockChunkPos.x, blockChunkPos.y, blockChunkPos.z, caveAir);
                chunk.SetDensity(blockChunkPos.x, blockChunkPos.y, blockChunkPos.z, caveBlock.density);

                if (caveBlock.isFloor && caveBlock.isFlat)
                {
                    chunk.SetBlockRaw(blockChunkPos.x, blockChunkPos.y - 1, blockChunkPos.z, terrGravel);
                    chunk.SetDensity(blockChunkPos.x, blockChunkPos.y - 1, blockChunkPos.z, MarchingCubes.DensityTerrain);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[Cave] {e.GetType()} (Chunk={caveBlock.chunkPos}, block={caveBlock.blockChunkPos})");
            }

            if (!caveBlock.isWater)
                continue;

            try
            {
                chunk.SetWaterRaw(blockChunkPos.x, blockChunkPos.y, blockChunkPos.z, WaterValue.Full);
            }
            catch (Exception e)
            {
                Log.Error($"[Cave] {e.GetType()} (Chunk={caveBlock.chunkPos}, block={caveBlock.blockChunkPos})");
            }
        }

        foreach (CaveBlock caveBlock in caveBlocks)
        {
            Vector3bf blockChunkPos = caveBlock.blockChunkPos;

            var worldX = chunkWorldPos.x + blockChunkPos.x;
            var worldZ = chunkWorldPos.z + blockChunkPos.z;

            var blockValue = SpawnDecoration(positions, caveBlock, random, worldX, blockChunkPos.y, worldZ);

            if (blockValue.type != caveAir.type)
            {
                chunk.SetBlock(GameManager.Instance.World, blockChunkPos.x, blockChunkPos.y, blockChunkPos.z, blockValue);
            }
        }
    }
}