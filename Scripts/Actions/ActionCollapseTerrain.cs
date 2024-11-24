using GameEvent.SequenceActions;
using System;
using System.Collections.Generic;
using System.Linq;

public class ActionCollapseTerrain : BaseAction
{
    private const string buffCaveTerrainEventCoolDownProp = "buffCaveTerrainEventCoolDown";

    public override ActionCompleteStates OnPerformAction()
    {
        var player = Owner.Target as EntityPlayer;
        var playerPos = new Vector3i(player.position);
        var blockUnderPlayer = GameManager.Instance.World.GetBlock(playerPos + Vector3i.down);

        bool isPlayerStandingOnTerrain = CaveBlocks.IsTerrain(blockUnderPlayer);

        if (isPlayerStandingOnTerrain && CollapseTerrain(player))
        {
            player.Buffs.AddBuff(buffCaveTerrainEventCoolDownProp);
        }

        return ActionCompleteStates.Complete;
    }

    private bool CollapseTerrain(EntityPlayer player)
    {
        /* NOTE:
        To reduce stability computations, collapse only the blocks directly
        under the player and set the blocks below to air.
        */

        var playerPos = new Vector3i(player.position);
        var positionsToFall = new HashSet<Vector3i>();
        var positionsToDestroy = new HashSet<Vector3i>();
        var random = new Random();

        if (GameManager.Instance.World.GetPOIAtPosition(playerPos, false) != null)
        {
            return false;
        }

        var flatPositions = FindFlatBlocks(playerPos + Vector3i.down);

        if (flatPositions.Count < 32)
        {
            return false;
        }

        positionsToFall.UnionWith(flatPositions);

        foreach (var pos in flatPositions)
        {
            positionsToFall.Add(new Vector3i(
                pos.x,
                pos.y + 1,
                pos.z
            ));

            float deep = 5;

            for (int y = 2; y <= deep; y++)
            {
                positionsToDestroy.Add(new Vector3i(
                    pos.x,
                    playerPos.y - y,
                    pos.z
                ));
            }
        }

        var blockChangeInfos = positionsToDestroy
            .Select(pos => new BlockChangeInfo(pos, BlockValue.Air, MarchingCubes.DensityAir))
            .ToList();

        GameManager.Instance.World.SetBlocksRPC(blockChangeInfos);
        GameManager.Instance.World.AddFallingBlocks(positionsToFall.ToList());

        return true;
    }

    private HashSet<Vector3i> FindFlatBlocks(Vector3i start)
    {
        var timer = CaveUtils.StartTimer();
        var world = GameManager.Instance.World;
        var queue = new Queue<Vector3i>();
        var visited = new HashSet<Vector3i>();
        var flatBlocks = new HashSet<Vector3i>();
        var neighbor = Vector3i.zero;
        var rolls = 0;

        queue.Enqueue(start);

        while (queue.Count > 0 && ++rolls < 100)
        {
            Vector3i pos = queue.Dequeue();

            foreach (var offset in CaveUtils.offsetsHorizontal8)
            {
                neighbor.x = pos.x + offset.x;
                neighbor.y = pos.y;
                neighbor.z = pos.z + offset.z;

                if (!visited.Contains(neighbor) && IsFlatBlock(neighbor, world))
                {
                    queue.Enqueue(neighbor);
                    flatBlocks.Add(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        Logging.Info($"terrainCollapse, rolls: {rolls}, timer: {timer.ElapsedMilliseconds}ms");

        return flatBlocks;
    }

    private static bool IsFlatBlock(Vector3i worldPos, World world, int radius = 1)
    {
        int x = worldPos.x;
        int y = worldPos.y;
        int z = worldPos.z;

        var block = world.GetBlock(x, y, z);
        var blockBelow = world.GetBlock(x, y - 1, z);
        var blockAbove = world.GetBlock(x, y + 1, z);

        bool isFlatBlock =
               CaveBlocks.IsTerrain(block)
            && CaveBlocks.IsTerrain(blockBelow)
            && !CaveBlocks.IsTerrain(blockAbove);

        return isFlatBlock;
    }

    public override BaseAction Clone()
    {
        return new ActionCollapseTerrain();
    }
}