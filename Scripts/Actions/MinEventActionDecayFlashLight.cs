
using UnityEngine;

public class MinEventActionDecayFlashLight : MinEventActionDecayLightAbstract
{
    public override ItemValue GetLightItemValue(MinEventParams _params)
    {
        return _params.Self.inventory.holdingItemItemValue;
    }

    public override Transform GetLightTransform(MinEventParams _params)
    {
        return _params.Self.inventory.GetHoldingItemTransform();
    }

    public override void AfterExecute(EntityAlive player)
    {
        // update the itemStack UI
        player.inventory.notifyListeners();
    }
}