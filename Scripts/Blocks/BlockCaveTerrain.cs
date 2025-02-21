using System.Collections.Generic;

public class BlockCaveTerrain : Block
{
    private static Dictionary<int, BlockValue> blockReplacements;

    public override DestroyedResult OnBlockDestroyedBy(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _entityId, bool _bUseHarvestTool)
    {
        UpdateNeighbors(_clrIdx, _blockPos);
        return DestroyedResult.Remove;
    }

    public override DestroyedResult OnBlockDestroyedByExplosion(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _playerThatStartedExpl)
    {
        UpdateNeighbors(_clrIdx, _blockPos);
        return base.OnBlockDestroyedByExplosion(_world, _clrIdx, _blockPos, _blockValue, _playerThatStartedExpl);
    }

    private void UpdateNeighbors(int _clrIdx, Vector3i _blockPos)
    {
        var blockChangeInfos = new List<BlockChangeInfo>();
        var neighborPos = new Vector3i();
        var world = GameManager.Instance.World;

        blockChangeInfos.Add(new BlockChangeInfo(_clrIdx, _blockPos, CaveBlocks.caveAir));

        foreach (var offset in BFSUtils.offsets)
        {
            neighborPos.x = _blockPos.x + offset.x;
            neighborPos.y = _blockPos.y + offset.y;
            neighborPos.z = _blockPos.z + offset.z;

            var neighbor = world.GetBlock(neighborPos);

            if (neighbor.isair || !CaveBlocks.IsTerrain(neighbor))
            {
                continue;
            }

            var blockValue = ReplaceBlock(neighbor);

            if (blockValue.isair)
            {
                continue;
            }

            blockChangeInfos.Add(new BlockChangeInfo(_clrIdx, neighborPos, blockValue));
        }

        world.SetBlocksRPC(blockChangeInfos);
    }

    private BlockValue ReplaceBlock(BlockValue blockValue)
    {
        if (blockReplacements is null)
        {
            InitReplacementBlocks();
        }

        if (blockReplacements.TryGetValue(blockValue.Block.blockID, out var result))
        {
            return result;
        }

        return BlockValue.Air;
    }

    private static void InitReplacementBlocks()
    {
        blockReplacements = new Dictionary<int, BlockValue>()
        {
            { GetBlockByName("terrGravel").blockID, CaveBlocks.caveTerrGravel },
            { GetBlockByName("terrOreCoal").blockID, CaveBlocks.caveTerrOreCoal },
            { GetBlockByName("terrOreIron").blockID, CaveBlocks.caveTerrOreIron },
            { GetBlockByName("terrOreLead").blockID, CaveBlocks.caveTerrOreLead },
            { GetBlockByName("terrOreOilDeposit").blockID, CaveBlocks.caveTerrOreOilDeposit },
            { GetBlockByName("terrOrePotassiumNitrate").blockID, CaveBlocks.caveTerrOrePotassiumNitrate },
            { GetBlockByName("terrSandStone").blockID, CaveBlocks.caveTerrSandStone },
            { GetBlockByName("terrSnow").blockID, CaveBlocks.caveTerrSnow },
            { GetBlockByName("terrStone").blockID, CaveBlocks.caveTerrStone},
        };
    }


}