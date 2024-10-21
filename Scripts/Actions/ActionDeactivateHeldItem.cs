using System.Collections;
using GameEvent.SequenceActions;
using UnityEngine;

class ActionDeactivateHeldItem : ActionBaseItemAction
{
    public override void OnClientPerform(Entity target)
    {
        var player = target as EntityPlayer;
        var transform = player.inventory.GetHoldingItemTransform();

        if (player.inventory.holdingItemItemValue.Activated == 0)
        {
            Log.Warning("[Cave] ActionDeactivateHeldItem: 'player.inventory.holdingItemItemValue.Activated' should not be at 0.");
            return;
        }

        if (transform != null)
        {
            GameManager.Instance.StartCoroutine(LightSparkleCoroutine(player, transform));
        }
        else
        {
            DeactivateFlashLight(player);
        }
    }

    private void DeactivateFlashLight(EntityPlayer player)
    {
        player.MinEventContext.ItemValue = player.inventory.holdingItemItemValue;
        player.inventory.holdingItemItemValue.FireEvent(MinEventTypes.onSelfItemDeactivate, player.MinEventContext);
        player.inventory.holdingItemItemValue.Activated = 0;
    }

    private IEnumerator LightSparkleCoroutine(EntityPlayer player, Transform transform)
    {
        SetLightActive(false, transform);
        yield return new WaitForSeconds(0.10f);

        SetLightActive(true, transform);
        yield return new WaitForSeconds(0.05f);

        SetLightActive(false, transform);
        yield return new WaitForSeconds(0.10f);

        SetLightActive(true, transform);
        yield return new WaitForSeconds(0.03f);

        SetLightActive(false, transform);
        yield return new WaitForSeconds(0.25f);

        SetLightActive(true, transform);
        yield return new WaitForSeconds(0.05f);

        DeactivateFlashLight(player);
        yield break;
    }

    private void SetLightActive(bool isActive, Transform transform)
    {
        if (transform != null)
        {
            Transform transform2 = GameUtils.FindDeepChild(transform, "lightSource");
            if (!(transform2 == null))
            {
                transform2.gameObject.SetActive(isActive);
                LightManager.LightChanged(transform2.position + Origin.position);
            }
        }
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
