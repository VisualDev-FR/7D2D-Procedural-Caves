using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;


public class CaveGenerator
{
    public static CaveChunksProvider caveChunksProvider;

    public static bool isEnabled = false;

    public static Vector3i HalfWorldSize;

    public static void Init(string worldName)
    {
        isEnabled = Directory.Exists($"{GameIO.GetWorldDir(worldName)}/cavemap");

        if (isEnabled)
        {
            int worldSize = GetWorldSize(worldName);
            caveChunksProvider = new CaveChunksProvider(worldName, worldSize);
            HalfWorldSize = new Vector3i(worldSize >> 1, 0, worldSize >> 1);

            Log.Out($"[Cave] init caveGenerator for world '{worldName}', size: {worldSize}");
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

    private static bool IsFlatFloor(Vector3i worldPos, int radius = 1)
    {
        int x0 = worldPos.x - radius;
        int z0 = worldPos.z - radius;
        int x1 = worldPos.x + radius;
        int z1 = worldPos.z + radius;

        int y = worldPos.y;

        for (int x = x0; x <= x1; x++)
        {
            for (int z = z0; z <= z1; z++)
            {
                if (!caveChunksProvider.IsCave(x, y, z) || caveChunksProvider.IsCave(x, y - 1, z))
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
                if (caveChunksProvider.IsCave(x, y, z))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static BlockValue SpawnDecoration(CaveBlock caveBlock, GameRandom random, int worldX, int worldY, int worldZ)
    {
        if (caveBlock.isRoom)
        {
            return CaveBlocks.caveAir;
        }

        random.InternalSetSeed(worldX * 13 + worldZ);

        Vector3i worldPos = new Vector3i(worldX, worldY, worldZ);

        bool lowerIsCave = caveChunksProvider.IsCave(worldX, worldY - 1, worldZ);
        bool upperIsCave = caveChunksProvider.IsCave(worldX, worldY + 1, worldZ);

        bool isFloor = !lowerIsCave && upperIsCave;
        bool isCeiling = lowerIsCave && !upperIsCave;
        bool isFlatFloor = IsFlatFloor(worldPos);
        bool isWater = caveBlock.isWater;

        BlockValue placeHolder;

        if (isFloor && isWater)
            // TODO: pimp water decoration
            placeHolder = CaveBlocks.cntCaveFloorFlat;

        else if (isFlatFloor && !isWater)
            placeHolder = CaveBlocks.cntCaveFloorFlat;

        else if (isFloor && !isWater)
            placeHolder = CaveBlocks.cntCaveFloor;

        else if (isCeiling)
            placeHolder = CaveBlocks.cntCaveCeiling;

        else
            return CaveBlocks.caveAir;

        int maxTries = 20;

        while (maxTries-- > 0)
        {
            var blockValue = BlockPlaceholderMap.Instance.Replace(placeHolder, random, worldX, worldZ);

            if (!isFlatFloor)
            {
                return blockValue;
            }
            else if (blockValue.type == CaveBlocks.caveAir.type)
            {
                return BlockPlaceholderMap.Instance.Replace(CaveBlocks.cntCaveFloor, random, worldX, worldZ);
            }
            else if (CanPlaceDecoration(blockValue, worldPos))
            {
                return blockValue;
            }
        }

        return CaveBlocks.caveAir;
    }

    private static BlockValue GetBoundingStoneBlockValue(int biomeID, BlockValue currentBlockValue)
    {
        /* biomes.xml

            <biomemap id="01" name="snow"/>
            <biomemap id="03" name="pine_forest"/>
            <biomemap id="05" name="desert"/>
            <biomemap id="06" name="water"/>
            <biomemap id="07" name="radiated"/>
            <biomemap id="08" name="wasteland"/>
            <biomemap id="09" name="burnt_forest"/>
            <biomemap id="13" name="caveFloor"/>
            <biomemap id="14" name="caveCeiling"/>
            <biomemap id="18" name="onlyWater"/>
            <biomemap id="19" name="underwater"/>
        */

        BlockValue biomeBasedBlockValue;

        switch (biomeID)
        {
            case 1:
                biomeBasedBlockValue = CaveBlocks.caveTerrSnow;
                break;

            case 5:
                biomeBasedBlockValue = CaveBlocks.caveTerrSandStone;
                break;

            default:
                biomeBasedBlockValue = CaveBlocks.caveTerrStone;
                break;
        }

        switch (currentBlockValue.Block.blockName)
        {
            case "terrStone":
            case "terrDirt":
            case "terrDestroyedStone":
            case "terrDestroyedWoodDebris":
            case "terrGravel":
            case "terrSand":
            case "terrSandStone":
            case "terrSnow":
            case "terrAsphalt":
            case "terrConcrete":
                return biomeBasedBlockValue;

            case "terrOreIron":
                return CaveBlocks.caveTerrOreIron;

            case "terrOreLead":
                return CaveBlocks.caveTerrOreLead;

            case "terrOreCoal":
                return CaveBlocks.caveTerrOreCoal;

            case "terrOrePotassiumNitrate":
                return CaveBlocks.caveTerrOrePotassiumNitrate;

            case "terrOreOilDeposit":
                return CaveBlocks.caveTerrOreOilDeposit;

            default:
                return BlockValue.Air;
        }
    }

    public static void GenerateCaveChunk(Chunk chunk)
    {
        if (chunk == null)
        {
            Log.Warning($"[Cave] Null chunk at {chunk.ChunkPos}");
            return;
        }

        GameRandom random = Utils.RandomFromSeedOnPos(chunk.ChunkPos.x, chunk.ChunkPos.z, GameManager.Instance.World.Seed);
        HashSet<CaveBlock> caveBlocks = caveChunksProvider.GetCaveBlocks(chunk);
        Vector3i chunkWorldPos = chunk.GetWorldPos();

        if (caveBlocks == null)
            return;

        var caveBlocksV3i = caveBlocks.Select(block => block.blockChunkPos.ToVector3i()).ToHashSet();
        var visited = new HashSet<int>();
        var neighbor = new Vector3i();

        foreach (CaveBlock caveBlock in caveBlocks)
        {
            Vector3bf blockChunkPos = caveBlock.blockChunkPos;

            chunk.SetBlockRaw(blockChunkPos.x, blockChunkPos.y, blockChunkPos.z, CaveBlocks.caveAir);
            chunk.SetDensity(blockChunkPos.x, blockChunkPos.y, blockChunkPos.z, caveBlock.density);

            if (IsFlatFloor(caveBlock.ToWorldPos(HalfWorldSize)))
            {
                chunk.SetBlockRaw(blockChunkPos.x, blockChunkPos.y - 1, blockChunkPos.z, CaveBlocks.caveTerrGravel);
                chunk.SetDensity(blockChunkPos.x, blockChunkPos.y - 1, blockChunkPos.z, MarchingCubes.DensityTerrain);
            }

            if (caveBlock.isWater)
            {
                chunk.SetWaterRaw(blockChunkPos.x, blockChunkPos.y, blockChunkPos.z, WaterValue.Full);
            }

            int biomeID = chunk.GetBiomeId(0, 0);

            foreach (var offset in CaveUtils.offsets)
            {
                neighbor.x = blockChunkPos.x + offset.x;
                neighbor.y = blockChunkPos.y + offset.y;
                neighbor.z = blockChunkPos.z + offset.z;

                if (
                       visited.Contains(neighbor.GetHashCode())
                    || caveBlocksV3i.Contains(neighbor)
                    || neighbor.x < 0
                    || neighbor.z < 0
                    || neighbor.x > 15
                    || neighbor.z > 15
                    )
                    continue;

                var currentBlock = chunk.GetBlock(neighbor);
                var blockValue = GetBoundingStoneBlockValue(biomeID, currentBlock);

                if (blockValue.isair)
                    continue;

                chunk.SetBlockRaw(neighbor.x, neighbor.y, neighbor.z, blockValue);
                chunk.SetDensity(neighbor.x, neighbor.y, neighbor.z, MarchingCubes.DensityTerrain);

                visited.Add(neighbor.GetHashCode());
            }
        }

        foreach (CaveBlock caveBlock in caveBlocks)
        {
            Vector3bf blockChunkPos = caveBlock.blockChunkPos;

            var worldX = chunkWorldPos.x + blockChunkPos.x;
            var worldZ = chunkWorldPos.z + blockChunkPos.z;

            var blockValue = SpawnDecoration(caveBlock, random, worldX, blockChunkPos.y, worldZ);

            if (blockValue.type != CaveBlocks.caveAir.type)
            {
                chunk.SetBlock(GameManager.Instance.World, blockChunkPos.x, blockChunkPos.y, blockChunkPos.z, blockValue);
            }
        }
    }
}