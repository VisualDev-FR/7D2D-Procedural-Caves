using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;


public class CaveGenerator
{
    private static CaveChunksProvider caveChunksProvider;

    private static Vector3i HalfWorldSize;

    public static bool isEnabled = false;

    private static int worldSize;

    public static void Init()
    {
        string worldName = GamePrefs.GetString(EnumGamePrefs.GameWorld);
        string caveMapDir = $"{GameIO.GetWorldDir(worldName)}/cavemap";

        if (Directory.Exists(caveMapDir))
        {
            worldSize = GetWorldSize(worldName);
            caveChunksProvider = new CaveChunksProvider(worldName, worldSize);
            HalfWorldSize = CaveUtils.HalfWorldSize(worldSize);
            isEnabled = true;

            Logging.Info($"init caveGenerator for world '{worldName}', size: {worldSize}");
        }
        else
        {
            isEnabled = false;
            Logging.Warning($"no cavemap found for world '{worldName}'");
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
            Logging.Warning($"Null chunk at {chunk.ChunkPos}");
            return;
        }

        HashSet<CaveBlock> caveBlocks = caveChunksProvider.GetCaveBlocks(chunk);

        if (caveBlocks == null)
            return;

        var visited = caveBlocks.Select(block => block.ToWorldPos(HalfWorldSize)).ToHashSet();
        var neighbor = new Vector3i();

        foreach (CaveBlock caveBlock in caveBlocks)
        {

            Vector3bf blockChunkPos = caveBlock.posInChunk;

            SetCaveBlock(chunk, caveBlock);

            if (caveBlock.skipDecoration)
                continue;

            int biomeID = chunk.GetBiomeId(0, 0);

            foreach (var offset in BFSUtils.offsets)
            {
                // TODO: neighborHash + offsetHash
                neighbor.x = blockChunkPos.x + offset.x;
                neighbor.y = blockChunkPos.y + offset.y;
                neighbor.z = blockChunkPos.z + offset.z;

                if (
                       visited.Contains(neighbor)
                    || neighbor.x < 0 || neighbor.x > 15
                    || neighbor.z < 0 || neighbor.z > 15
                    )
                    continue;

                var currentBlock = chunk.GetBlock(neighbor);
                var blockValue = GetBoundingBlockValue(biomeID, currentBlock, caveBlock.isWater && neighbor.y < caveBlock.y);

                if (blockValue.isair)
                    continue;

                chunk.SetBlockRaw(neighbor.x, neighbor.y, neighbor.z, blockValue);
                chunk.SetDensity(neighbor.x, neighbor.y, neighbor.z, MarchingCubes.DensityTerrain);

                visited.Add(neighbor);
            }
        }

        DecorateChunk(chunk, caveBlocks);
    }

    private static void SetCaveBlock(Chunk chunk, CaveBlock caveBlock)
    {
        var blockPosInChunk = caveBlock.posInChunk;
        var blockValue = CaveBlocks.caveAir;

        if (caveBlock.isRope)
        {
            blockValue = CaveBlocks.climbableRopeBlock;
        }

        chunk.SetBlockRaw(blockPosInChunk.x, blockPosInChunk.y, blockPosInChunk.z, blockValue);
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
        var decoratedPositions = new HashSet<Vector3i>();

        foreach (CaveBlock caveBlock in caveBlocks)
        {
            if (!caveBlock.skipDecoration)
            {
                TrySpawnCaveDecoration(random, chunk, caveBlock, decoratedPositions);
            }
        }
    }

    private static string GetBiomeName(int worldX, int worldZ)
    {
        var biomeDefinition = GameManager.Instance.World.ChunkCache.ChunkProvider
            .GetBiomeProvider()
            .GetBiomeAt(worldX, worldZ);

        if (biomeDefinition is null)
            return "";

        return biomeDefinition.m_sBiomeName;
    }

    private static bool IsPlaceholderEmpty(BlockValue placeholder)
    {
        if (BlockPlaceholderMap.Instance.placeholders.TryGetValue(placeholder, out var placeholderTargets))
        {
            return placeholderTargets.Count == 0;
        }

        return true;
    }

    private static BlockValue GetBiomePlaceholder(string biomeName, string placeholderName)
    {
        var placeholder = CaveBlocks.TryGetBlockValue($"{placeholderName}_{biomeName}");

        if (!placeholder.isair && !IsPlaceholderEmpty(placeholder))
        {
            return placeholder;
        }

        return CaveBlocks.GetBlockValue(placeholderName);
    }

    private static BlockValue GetDecorationPlaceholder(string biomeName, bool isFloor, bool isWater, bool isFlatFloor, bool isCeiling)
    {
        if (isFlatFloor && !isWater)
            return GetBiomePlaceholder(biomeName, "cntCaveFloorFlat");

        if (isFloor && !isWater)
            return GetBiomePlaceholder(biomeName, "cntCaveFloor");

        if (isCeiling && !isWater)
            return GetBiomePlaceholder(biomeName, "cntCaveCeiling");

        if (isFlatFloor && isWater)
            return GetBiomePlaceholder(biomeName, "cntCaveFloorFlatWater");

        if (isFloor && isWater)
            return GetBiomePlaceholder(biomeName, "cntCaveFloorWater");

        if (isCeiling && isWater)
            return GetBiomePlaceholder(biomeName, "cntCaveCeilingWater");

        return BlockValue.Air;
    }

    private static void TrySpawnCaveDecoration(GameRandom random, Chunk chunk, CaveBlock caveBlock, HashSet<Vector3i> decoratedPositions)
    {
        Vector3i worldPos = caveBlock.ToWorldPos(HalfWorldSize);

        int worldX = worldPos.x;
        int worldY = worldPos.y;
        int worldZ = worldPos.z;

        bool lowerIsCave = caveChunksProvider.IsCave(worldX, worldY - 1, worldZ);
        bool upperIsCave = caveChunksProvider.IsCave(worldX, worldY + 1, worldZ);

        bool isFloor = !lowerIsCave && upperIsCave;
        bool isCeiling = lowerIsCave && !upperIsCave;
        bool isFlatFloor = IsFlatFloor(worldPos);
        bool isWater = caveBlock.isWater;

        var biomeName = GetBiomeName(worldPos.x, worldPos.z).ToLower();
        var placeHolder = GetDecorationPlaceholder(biomeName, isFloor, isWater, isFlatFloor, isCeiling);

        if (placeHolder.isair)
            return;

        var blockValue = CaveBlocks.caveAir;
        int maxTries = 20;

        while (maxTries-- > 0)
        {
            blockValue = BlockPlaceholderMap.Instance.Replace(placeHolder, random, worldX, worldZ);
            blockValue.rotation = (byte)random.Next(4);

            if (isFloor && !CanPlaceFloorDecoration(chunk, blockValue, worldPos, decoratedPositions))
            {
                blockValue = CaveBlocks.caveAir;
            }
            else
            {
                break;
            }
        }

        if (blockValue.type == CaveBlocks.caveAir.type)
            return;

        int yOffset = 0;

        if (isCeiling && blockValue.Block.isMultiBlock)
        {
            yOffset = 1 - blockValue.Block.multiBlockPos.dim.y;
        }

        chunk.SetBlock(
            GameManager.Instance.World,
            caveBlock.posInChunk.x,
            caveBlock.posInChunk.y + yOffset,
            caveBlock.posInChunk.z,
            blockValue
        );

        decoratedPositions.UnionWith(GetDecoratedPositions(blockValue, worldPos));
    }

    private static IEnumerable<Vector3i> GetDecoratedPositions(BlockValue blockValue, Vector3i worldPos)
    {
        if (!blockValue.Block.isMultiBlock)
        {
            yield return worldPos;
            yield break;
        }

        var bounds = GetRotatedBlockBounds(blockValue, worldPos);
        var position = Vector3i.zero;

        for (int x = bounds.min.x; x <= bounds.max.x; x++)
        {
            for (int z = bounds.min.z; z <= bounds.max.z; z++)
            {
                position.x = x;
                position.y = worldPos.y;
                position.z = z;

                yield return position;
            }
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

    private static bool CanPlaceFloorDecoration(Chunk chunk, BlockValue blockValue, Vector3i worldPos, HashSet<Vector3i> decoratedPositions)
    {
        var bounds = GetRotatedBlockBounds(blockValue, worldPos);

        // int margin = blockValue.Block.isMultiBlock ? 1 : 0;
        int margin = 0;

        int x0 = bounds.xMin - margin;
        int z0 = bounds.zMin - margin;
        int x1 = bounds.xMax + margin;
        int z1 = bounds.zMax + margin;

        int y = worldPos.y;

        // Logging.Debug($"{blockValue.Block.blockName}, position: {worldPos} rotation: {blockValue.rotation}, [{x0},{z0} -> {x1},{z1}]");

        var position = Vector3i.zero;

        for (int x = x0; x <= x1; x++)
        {
            for (int z = z0; z <= z1; z++)
            {
                position.x = x;
                position.y = y;
                position.z = z;

                bool isAirBelow = caveChunksProvider.IsCave(x, y - 1, z);
                bool isCaveBlock = caveChunksProvider.IsCave(x, y, z);
                bool isAlreadyDecorated = decoratedPositions.Contains(position);

                // Logging.Debug($"---- {x},{y},{z}: isAirBelow: {isAirBelow}, isCaveBlock: {isCaveBlock}, isAlreadyDecorated: {isAlreadyDecorated}");

                if (isAirBelow || (isCaveBlock && isAlreadyDecorated))
                {
                    // Logging.Debug("xxxx invalid placement");
                    return false;
                }
            }
        }

        // Logging.Debug("++++ valid placement");

        return true;
    }

    private static BoundsInt GetRotatedBlockBounds(BlockValue blockValue, Vector3i worldPos)
    {
        var bounds = new BoundsInt();

        if (!blockValue.Block.isMultiBlock)
        {
            bounds.SetMinMax(worldPos, worldPos);
            return bounds;
        }

        var size = blockValue.Block.multiBlockPos.dim - Vector3i.one;

        switch (blockValue.rotation)
        {
            case 0:
                bounds.SetMinMax(
                    worldPos - size,
                    worldPos
                );
                break;

            case 1:
                bounds.xMin = worldPos.x - size.x;
                bounds.xMax = worldPos.x;
                bounds.zMin = worldPos.z;
                bounds.zMax = worldPos.z + size.z;
                break;

            case 2:
                bounds.SetMinMax(
                    worldPos,
                    worldPos + size
                );
                break;

            case 3:
                bounds.xMin = worldPos.x;
                bounds.xMax = worldPos.x + size.x;
                bounds.zMin = worldPos.z - size.z;
                bounds.zMax = worldPos.z;
                break;

            default:
                throw new Exception($"Invalid rotation: {blockValue.rotation}");
        }

        bounds.yMin = worldPos.y;
        bounds.yMax = worldPos.y + size.y;

        return bounds;
    }

    private static BlockValue GetBoundingBlockValue(int biomeID, BlockValue currentBlockValue, bool isWater)
    {
        if (isWater)
        {
            return CaveBlocks.caveTerrGravel;
        }
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