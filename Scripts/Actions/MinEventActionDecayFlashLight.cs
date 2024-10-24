
using UnityEngine;

public class MinEventActionDecayFlashLight : MinEventActionDecayLight
{
    public override ItemValue GetLightItemValue(MinEventParams _params)
    {
        return _params.Self.inventory.holdingItemItemValue;
    }

    public override Transform GetLightTransform(MinEventParams _params)
    {
        return _params.Self.inventory.GetHoldingItemTransform();
    }
}