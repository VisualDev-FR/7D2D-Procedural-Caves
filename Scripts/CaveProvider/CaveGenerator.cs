using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;


public class CaveGenerator
{
    public static CaveChunksProvider caveChunksProvider;

    public static Vector3i HalfWorldSize;

    public static bool isEnabled = false;

    public static void Init(string worldName)
    {
        string caveMapDir = $"{GameIO.GetWorldDir(worldName)}/cavemap";

        if (Directory.Exists(caveMapDir))
        {
            int worldSize = GetWorldSize(worldName);
            caveChunksProvider = new CaveChunksProvider(worldName, worldSize);
            HalfWorldSize = CaveUtils.HalfWorldSize(worldSize);
            isEnabled = true;

            Log.Out($"[Cave] init caveGenerator for world '{worldName}', size: {worldSize}");
        }
        else
        {
            isEnabled = false;
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

    public static void GenerateCaveChunk(Chunk chunk)
    {
        if (chunk == null)
        {
            Log.Warning($"[Cave] Null chunk at {chunk.ChunkPos}");
            return;
        }

        HashSet<CaveBlock> caveBlocks = caveChunksProvider.GetCaveBlocks(chunk);

        if (caveBlocks == null)
            return;

        var caveBlocksV3i = caveBlocks.Select(block => block.posInChunk.ToVector3i()).ToHashSet();
        var visited = new HashSet<int>();
        var neighbor = new Vector3i();

        foreach (CaveBlock caveBlock in caveBlocks)
        {
            Vector3bf blockChunkPos = caveBlock.posInChunk;

            SetCaveBlock(chunk, caveBlock);

            int biomeID = chunk.GetBiomeId(0, 0);

            foreach (var offset in CaveUtils.offsets)
            {
                // TODO: neighborHash + offsetHash
                neighbor.x = blockChunkPos.x + offset.x;
                neighbor.y = blockChunkPos.y + offset.y;
                neighbor.z = blockChunkPos.z + offset.z;

                if (
                       visited.Contains(neighbor.GetHashCode())
                    || caveBlocksV3i.Contains(neighbor)
                    || neighbor.x < 0   // TODO: (neighbor.x & 0b1111) == neighbor.x
                    || neighbor.z < 0   // TODO: (neighbor.z & 0b1111) == neighbor.z
                    || neighbor.x > 15
                    || neighbor.z > 15
                    )
                    continue;

                var currentBlock = chunk.GetBlock(neighbor);
                var blockValue = GetBoundingBlockValue(biomeID, currentBlock);

                if (blockValue.isair)
                    continue;

                chunk.SetBlockRaw(neighbor.x, neighbor.y, neighbor.z, blockValue);
                chunk.SetDensity(neighbor.x, neighbor.y, neighbor.z, MarchingCubes.DensityTerrain);

                visited.Add(neighbor.GetHashCode());
            }
        }

        DecorateChunk(chunk, caveBlocks);
    }

    private static void SetCaveBlock(Chunk chunk, CaveBlock caveBlock)
    {
        var blockPosInChunk = caveBlock.posInChunk;

        chunk.SetBlockRaw(blockPosInChunk.x, blockPosInChunk.y, blockPosInChunk.z, CaveBlocks.caveAir);
        chunk.SetDensity(blockPosInChunk.x, blockPosInChunk.y, blockPosInChunk.z, caveBlock.density);

        if (IsFlatFloor(caveBlock.ToWorldPos(HalfWorldSize)))
        {
            chunk.SetBlockRaw(blockPosInChunk.x, blockPosInChunk.y - 1, blockPosInChunk.z, CaveBlocks.caveTerrGravel);
            chunk.SetDensity(blockPosInChunk.x, blockPosInChunk.y - 1, blockPosInChunk.z, MarchingCubes.DensityTerrain);
        }

        if (caveBlock.isWater)
        {
            chunk.SetWaterRaw(blockPosInChunk.x, blockPosInChunk.y, blockPosInChunk.z, WaterValue.Full);
        }
    }

    private static void DecorateChunk(Chunk chunk, IEnumerable<CaveBlock> caveBlocks)
    {
        var random = GameRandomManager.instance.CreateGameRandom(chunk.GetHashCode());

        foreach (CaveBlock caveBlock in caveBlocks)
        {
            SpawnCaveDecorationAt(random, chunk, caveBlock);
        }
    }

    private static void SpawnCaveDecorationAt(GameRandom random, Chunk chunk, CaveBlock caveBlock)
    {
        Vector3bf posInChunk = caveBlock.posInChunk;
        Vector3i chunkWorldPos = chunk.GetWorldPos();

        int worldX = chunkWorldPos.x + posInChunk.x;
        int worldY = posInChunk.y;
        int worldZ = chunkWorldPos.z + posInChunk.z;

        Vector3i worldPos = new Vector3i(worldX, worldY, worldZ);

        bool lowerIsCave = caveChunksProvider.IsCave(worldX, worldY - 1, worldZ);
        bool upperIsCave = caveChunksProvider.IsCave(worldX, worldY + 1, worldZ);

        bool isFloor = !lowerIsCave && upperIsCave;
        bool isCeiling = lowerIsCave && !upperIsCave;
        bool isFlatFloor = IsFlatFloor(worldPos);
        bool isWater = caveBlock.isWater;

        BlockValue placeHolder;

        if (isFlatFloor && isWater)
            placeHolder = CaveBlocks.cntCaveFlatWater;

        else if (isFlatFloor && !isWater)
            placeHolder = CaveBlocks.cntCaveFloorFlat;

        else if (isFloor && !isWater)
            placeHolder = CaveBlocks.cntCaveFloor;

        else if (isCeiling)
            placeHolder = CaveBlocks.cntCaveCeiling;

        else
            return;

        var blockValue = BlockValue.Air;
        int maxTries = 20;

        while (maxTries-- > 0)
        {
            blockValue = BlockPlaceholderMap.Instance.Replace(placeHolder, random, worldX, worldZ);

            if (isFlatFloor && blockValue.type == CaveBlocks.caveAir.type)
            {
                blockValue = BlockPlaceholderMap.Instance.Replace(CaveBlocks.cntCaveFloor, random, worldX, worldZ);
                break;
            }
            else if (isFlatFloor && CanPlaceFloorDecoration(chunk, blockValue, worldPos))
            {
                break;
            }
        }

        if (blockValue.Equals(CaveBlocks.caveAir))
            return;

        int yOffset = 0;

        if (isCeiling && blockValue.Block.isMultiBlock)
        {
            yOffset = 1 - blockValue.Block.multiBlockPos.dim.y;
        }

        blockValue.rotation = (byte)random.Next(4);
        chunk.SetBlock(GameManager.Instance.World, posInChunk.x, posInChunk.y + yOffset, posInChunk.z, blockValue);
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

    private static bool CanPlaceFloorDecoration(Chunk chunk, BlockValue blockValue, Vector3i worldPos)
    {
        var size = Vector3i.zero;

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

        int x0 = worldPos.x;
        int z0 = worldPos.z;
        int x1 = worldPos.x + size.x;
        int z1 = worldPos.z + size.z;
        int y = worldPos.y - 1;

        for (int x = x0; x <= x1; x++)
        {
            for (int z = z0; z <= z1; z++)
            {
                // check if there is terrain under the block we are attempting to place
                if (caveChunksProvider.IsCave(x, y, z))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static BlockValue GetBoundingBlockValue(int biomeID, BlockValue currentBlockValue)
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

}