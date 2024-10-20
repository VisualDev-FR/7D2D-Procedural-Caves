using GameEvent.SequenceActions;

class ActionDeactivateHeldItem : ActionBaseItemAction
{
    public override void OnClientPerform(Entity target)
    {
        var player = target as EntityPlayer;

        player.MinEventContext.ItemValue = player.inventory.holdingItemItemValue;
        player.inventory.holdingItemItemValue.FireEvent(MinEventTypes.onSelfItemDeactivate, player.MinEventContext);
        player.inventory.holdingItemItemValue.Activated = 0;
    }

    public override BaseAction CloneChildSettings()
    {
        return new ActionDeactivateHeldItem();
    }
}

// [HarmonyPatch(typeof(Inventory), "OnUpdate")]
// public class Inventory_updateHoldingItem
// {
//     // code --goto C:\tools\DEV\7D2D_Modding\7D2D-sources\Decompiled\Inventory.cs:483

//     public static bool Prefix(Inventory __instance)
//     {
//         var itemData = __instance.holdingItemData;
//         var itemValue = __instance.holdingItemItemValue;

//         Log.Out($"[Cave] updateHoldingItem: {itemData.item.Name}, active: {itemValue.Activated}, useTimes: {itemValue.UseTimes}");
//         return true;
//     }
// }
