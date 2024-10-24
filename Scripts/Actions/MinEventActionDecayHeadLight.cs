using UnityEngine;

public class MinEventActionDecayHeadLight : MinEventActionDecayLightAbstract
{
    private const string headLightPropFPV = "HeadLight";

    private const string headLightPropTPV = "HeadLightCam";

    private const string modArmorHelmetLightProp = "modArmorHelmetLight";

    public override bool CanExecute(MinEventTypes _eventType, MinEventParams _params)
    {
        var player = _params.Self;

        if (player == null || player.parts == null || (!player.parts.ContainsKey(headLightPropFPV) && !player.parts.ContainsKey(headLightPropTPV)))
        {
            Log.Out("no headlight on player");
            return false;
        }

        return base.CanExecute(_eventType, _params);
    }

    public override ItemValue GetLightItemValue(MinEventParams _params)
    {
        if (TryGetHeadLight(_params.Self, out var headLightItemValue))
        {
            return headLightItemValue;
        }

        return null;
    }

    public override Transform GetLightTransform(MinEventParams _params)
    {
        if (!(_params.Self is EntityPlayerLocal player))
        {
            return null;
        }

        if (player.bFirstPersonView && player.parts.TryGetValue(headLightPropFPV, out var fpvTransform))
        {
            return fpvTransform;
        }

        if (!player.bFirstPersonView && player.parts.TryGetValue(headLightPropTPV, out var tpvTransform))
        {
            return tpvTransform;
        }

        return null;
    }

    private bool TryGetHeadLight(EntityAlive player, out ItemValue headLightMod)
    {
        headLightMod = null;

        foreach (var itemValue in player.equipment.GetItems())
        {
            if (TryGetHeadlightItemValue(itemValue, ref headLightMod))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetHeadlightItemValue(ItemValue equipment, ref ItemValue headlightMod)
    {
        if (equipment is null || !equipment.HasMods() || equipment.Modifications == null)
            return false;

        foreach (var mod in equipment.Modifications)
        {
            if (mod.ItemClass.Name == modArmorHelmetLightProp)
            {
                headlightMod = mod;
                return true;
            }
        }

        return false;
    }
}