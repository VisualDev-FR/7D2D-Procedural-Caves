using Audio;
using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(ItemActionPlaceAsBlock), "ExecuteAction")]
public class ItemActionPlaceAsBlock_ExecuteAction
{
    public static void Prefix(ItemActionPlaceAsBlock __instance, ItemActionData _actionData, bool _bReleased)
    {
        if (!_bReleased || Time.time - _actionData.lastUseTime < __instance.Delay || Time.time - _actionData.lastUseTime < Constants.cBuildIntervall)
        {
            return;
        }
        EntityAlive holdingEntity = _actionData.invData.holdingEntity;
        if (EffectManager.GetValue(PassiveEffects.DisableItem, holdingEntity.inventory.holdingItemItemValue, 0f, holdingEntity, null, _actionData.invData.item.ItemTags) > 0f)
        {
            _actionData.lastUseTime = Time.time + 1f;
            Manager.PlayInsidePlayerHead("twitch_no_attack");
            return;
        }
        ItemInventoryData invData = _actionData.invData;
        Vector3i lastBlockPos = invData.hitInfo.lastBlockPos;
        if (!invData.hitInfo.bHitValid || lastBlockPos == Vector3i.zero || invData.hitInfo.tag.StartsWith("E_"))
        {
            return;
        }
        BlockValue block = invData.world.GetBlock(lastBlockPos);

        // NOTE: Patch this condition to allow item placement like torchs
        bool isAir = block.isair || block.type == CaveGenerator.caveAir.type;

        if (!isAir || (!invData.world.IsEditor() && GameUtils.IsColliderWithinBlock(lastBlockPos, block)))
        {
            return;
        }
        BlockValue blockValue = invData.item.OnConvertToBlockValue(invData.itemValue, __instance.blockToPlace);
        WorldRayHitInfo worldRayHitInfo = invData.hitInfo.Clone();
        worldRayHitInfo.hit.blockPos = lastBlockPos;
        int placementDistanceSq = blockValue.Block.GetPlacementDistanceSq();
        if (invData.hitInfo.hit.distanceSq > (float)placementDistanceSq)
        {
            return;
        }
        if (!blockValue.Block.CanPlaceBlockAt(invData.world, worldRayHitInfo.hit.clrIdx, lastBlockPos, blockValue))
        {
            GameManager.ShowTooltip(invData.holdingEntity as EntityPlayerLocal, "blockCantPlaced");
            return;
        }
        BlockPlacement.Result _bpResult = blockValue.Block.BlockPlacementHelper.OnPlaceBlock(BlockPlacement.EnumRotationMode.Auto, 0, invData.world, blockValue, worldRayHitInfo.hit, invData.holdingEntity.position);
        blockValue.Block.OnBlockPlaceBefore(invData.world, ref _bpResult, invData.holdingEntity, invData.world.GetGameRandom());
        blockValue = _bpResult.blockValue;
        if (blockValue.Block.IndexName == "lpblock")
        {
            if (!invData.world.CanPlaceLandProtectionBlockAt(_bpResult.blockPos, invData.world.gameManager.GetPersistentLocalPlayer()))
            {
                invData.holdingEntity.PlayOneShot("keystone_build_warning");
                return;
            }
            invData.holdingEntity.PlayOneShot("keystone_placed");
        }
        else if (!invData.world.CanPlaceBlockAt(_bpResult.blockPos, invData.world.gameManager.GetPersistentLocalPlayer()))
        {
            invData.holdingEntity.PlayOneShot("keystone_build_warning");
            return;
        }
        _actionData.lastUseTime = Time.time;
        blockValue.Block.PlaceBlock(invData.world, _bpResult, invData.holdingEntity);
        _actionData.invData.holdingEntity.MinEventContext.ItemActionData = _actionData;
        _actionData.invData.holdingEntity.MinEventContext.BlockValue = blockValue;
        _actionData.invData.holdingEntity.MinEventContext.Position = _bpResult.pos;
        _actionData.invData.holdingEntity.FireEvent(MinEventTypes.onSelfPlaceBlock);
        QuestEventManager.Current.BlockPlaced(blockValue.Block.GetBlockName(), _bpResult.blockPos);
        invData.holdingEntity.RightArmAnimationUse = true;
        if (__instance.changeItemTo != null)
        {
            ItemValue itemValue = ItemClass.GetItem(__instance.changeItemTo);
            if (!itemValue.IsEmpty())
            {
                invData.holdingEntity.inventory.SetItem(invData.holdingEntity.inventory.holdingItemIdx, new ItemStack(itemValue, 1));
            }
        }
        else
        {
            GameManager.Instance.StartCoroutine(__instance.decInventoryLater(invData, invData.holdingEntity.inventory.holdingItemIdx));
        }
        invData.holdingEntity.PlayOneShot((__instance.soundStart != null) ? __instance.soundStart : "placeblock");
        (invData.holdingEntity as EntityPlayerLocal).DropTimeDelay = 0.5f;
    }
}
