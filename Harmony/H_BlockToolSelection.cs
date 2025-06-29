using System.Collections.Generic;
using HarmonyLib;


[HarmonyPatch(typeof(BlockToolSelection), "CheckKeys")]
public class BlockToolSelection_CheckKeys
{
    public static void CheckKeys(BlockToolSelection instance, ItemInventoryData _data, WorldRayHitInfo _hitInfo, PlayerActionsLocal playerActions)
    {
        if (LocalPlayerUI.primaryUI.windowManager.IsInputActive())
        {
            return;
        }

        // NOTE: Patch to clear selection from BlockSelectionUtils
        // -------------------------------
        if (playerActions.SelectionDelete.IsPressed)
        {
            BlockSelectionUtils.ClearSelection();
        }
        // -------------------------------


        instance.hitInfo = _hitInfo;
        Vector3i vector3i = _data.world.IsEditor() && playerActions.Run.IsPressed ? _hitInfo.hit.blockPos : _hitInfo.lastBlockPos;

        if (_data is ItemClassBlock.ItemBlockInventoryData itemBlockInventoryData)
        {
            BlockValue bv = itemBlockInventoryData.itemValue.ToBlockValue();
            bv.rotation = itemBlockInventoryData.rotation;
            itemBlockInventoryData.rotation = bv.Block.BlockPlacementHelper.OnPlaceBlock(itemBlockInventoryData.mode, itemBlockInventoryData.localRot, GameManager.Instance.World, bv, instance.hitInfo.hit, itemBlockInventoryData.holdingEntity.position).blockValue.rotation;
        }
        if (!GameManager.Instance.IsEditMode() && !GamePrefs.GetBool(EnumGamePrefs.DebugMenuEnabled))
        {
            return;
        }
        if (playerActions.SelectionSet.IsPressed)
        {
            if (GameManager.Instance.World.ChunkClusters[_hitInfo.hit.clrIdx] == null || InputUtils.ControlKeyPressed)
            {
                return;
            }
            instance.SelectionLockMode = 0;
            instance.SelectionClrIdx = _hitInfo.hit.clrIdx;
            Vector3i vector3i2 = vector3i;
            if (!instance.SelectionActive)
            {
                Vector3i selectionSize = instance.SelectionSize;
                instance.SelectionStart = vector3i2;
                if (instance.SelectionLockMode == 1)
                {
                    instance.SelectionEnd = instance.SelectionStart + selectionSize - Vector3i.one;
                }
                else
                {
                    instance.SelectionEnd = instance.SelectionStart;
                }
                instance.SelectionActive = true;
            }
            else
            {
                instance.SelectionEnd = vector3i2;
            }
        }

        // NOTE: patch allowing to access the density modifier out of the edit mode
        // if (!GameManager.Instance.IsEditMode())
        // {
        //     return;
        // }

        if (playerActions.DensityM1.WasPressed || playerActions.DensityP1.WasPressed || playerActions.DensityM10.WasPressed || playerActions.DensityP10.WasPressed)
        {
            int num = ((playerActions.DensityM1.WasPressed || playerActions.DensityP1.WasPressed) ? 1 : 10);
            if (playerActions.DensityM1.WasPressed || playerActions.DensityM10.WasPressed)
            {
                num = -num;
            }
            if (InputUtils.ControlKeyPressed)
            {
                num *= 50;
            }
            BlockValue block = GameManager.Instance.World.GetBlock(_hitInfo.hit.clrIdx, vector3i);
            Block block2 = block.Block;
            if (block2.BlockTag == BlockTags.Door)
            {
                if (num > 0)
                {
                    num = ((block.damage + num >= block2.MaxDamagePlusDowngrades) ? (block2.MaxDamagePlusDowngrades - block.damage - 1) : num);
                }
                block2.DamageBlock(GameManager.Instance.World, _hitInfo.hit.clrIdx, vector3i, block, num, -1);
            }
            else
            {
                int num2 = (instance.SelectionActive ? GameManager.Instance.World.GetDensity(0, instance.m_selectionStartPoint) : GameManager.Instance.World.GetDensity(_hitInfo.hit.clrIdx, vector3i));
                num2 += num;
                num2 = Utils.FastClamp(num2, MarchingCubes.DensityTerrain, MarchingCubes.DensityAir);
                if (!instance.SelectionActive)
                {
                    GameManager.Instance.World.SetBlocksRPC(new List<BlockChangeInfo>
                    {
                        new BlockChangeInfo(_hitInfo.hit.clrIdx, vector3i, (sbyte)num2)
                    });
                }
                else
                {
                    BlockTools.CubeDensityRPC(GameManager.Instance, instance.m_selectionStartPoint, instance.m_SelectionEndPoint, (sbyte)num2);
                }
            }
        }
        if ((!playerActions.FocusCopyBlock.WasPressed && (!playerActions.Secondary.WasPressed || !InputUtils.ControlKeyPressed)) || !GameManager.Instance.IsEditMode() || !_hitInfo.bHitValid || _hitInfo.hit.blockValue.isair)
        {
            return;
        }
        EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
        BlockValue blockValue = _hitInfo.hit.blockValue;
        if (blockValue.ischild)
        {
            Vector3i parentPos = blockValue.Block.multiBlockPos.GetParentPos(_hitInfo.hit.blockPos, blockValue);
            blockValue = GameManager.Instance.World.GetBlock(parentPos);
        }
        ItemStack itemStack = new ItemStack(blockValue.ToItemValue(), 99);
        if (blockValue.Block.GetAutoShapeType() != EAutoShapeType.Helper)
        {
            var textureFull = GameManager.Instance.World.GetTextureFullArray(_hitInfo.hit.blockPos.x, _hitInfo.hit.blockPos.y, _hitInfo.hit.blockPos.z);
            itemStack.itemValue.TextureFullArray = textureFull;
        }
        if (primaryPlayer.inventory.GetItemCount(itemStack.itemValue, _bConsiderTexture: true) == 0 && primaryPlayer.inventory.CanTakeItem(itemStack))
        {
            if (primaryPlayer.inventory.AddItem(itemStack, out var _slot) && primaryPlayer.inventory.GetItemDataInSlot(_slot) is ItemClassBlock.ItemBlockInventoryData itemBlockInventoryData2)
            {
                itemBlockInventoryData2.damage = blockValue.damage;
            }
        }
        else if (_data is ItemClassBlock.ItemBlockInventoryData itemBlockInventoryData3 && instance.hasSameShape(blockValue.type, primaryPlayer.inventory.holdingItemItemValue.type))
        {
            itemBlockInventoryData3.rotation = blockValue.rotation;
            itemBlockInventoryData3.damage = blockValue.damage;
        }
    }

    public static bool Prefix(ItemInventoryData _data, WorldRayHitInfo _hitInfo, PlayerActionsLocal playerActions, BlockToolSelection __instance)
    {
        CheckKeys(__instance, _data, _hitInfo, playerActions);
        return false;
    }
}
