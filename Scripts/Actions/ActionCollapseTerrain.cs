using GameEvent.SequenceActions;
using System.Collections.Generic;
using System.Linq;

public class ActionCollapseTerrain : BaseAction
{
    private const string buffCaveTerrainEventCoolDownProp = "buffCaveTerrainEventCoolDown";

    public override ActionCompleteStates OnPerformAction()
    {
        Log.Out($"[Cave] ActionCollapseTerrain");

        var player = Owner.Target as EntityPlayer;
        var playerPos = new Vector3i(player.position);
        var blockUnderPlayer = GameManager.Instance.World.GetBlock(playerPos + Vector3i.down);

        bool isPlayerStandingOnTerrain = CaveBlocks.IsTerrain(blockUnderPlayer);

        Log.Out($"[Cave] isPlayerStandingOnTerrain: {isPlayerStandingOnTerrain}");

        if (isPlayerStandingOnTerrain && CollapseTerrain(playerPos))
        {
            player.Buffs.AddBuff(buffCaveTerrainEventCoolDownProp);
        }

        return ActionCompleteStates.Complete;
    }

    private bool CollapseTerrain(Vector3i start)
    {
        /* NOTE:
        To reduce stability computations, collapse only the blocks directly
        under the player and set the blocks below to air.
        */

        var blocksToCollapse = new List<Vector3i>();
        var blocksToDestroy = new List<Vector3i>();
        var position = Vector3i.zero;
        var deep = 5;
        var radius = 5;

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                position.x = start.x + x;
                position.y = Utils.FastMax(2, start.y - 1);
                position.z = start.z + z;

                blocksToCollapse.Add(position);

                for (int y = -2; y >= -deep; y--)
                {
                    position.y = Utils.FastMax(2, start.y + y);
                    blocksToDestroy.Add(position);

                    if (!CaveBlocks.IsTerrain(GameManager.Instance.World.GetBlock(position)))
                    {
                        return false;
                    }
                }
            }
        }

        var blockChangeInfos = blocksToDestroy
            .Select(blockPosition => new BlockChangeInfo(blockPosition, BlockValue.Air, MarchingCubes.DensityAir))
            .ToList();

        GameManager.Instance.World.SetBlocksRPC(blockChangeInfos);
        GameManager.Instance.World.AddFallingBlocks(blocksToCollapse);

        return true;
    }

    public override BaseAction Clone()
    {
        return new ActionCollapseTerrain();
    }
}