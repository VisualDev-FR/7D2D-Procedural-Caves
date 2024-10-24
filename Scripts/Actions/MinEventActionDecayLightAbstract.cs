using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = System.Random;


public abstract class MinEventActionDecayLightAbstract : MinEventActionTargetedBase
{
    // values in seconds, which say when the sparkle effect must be played
    // follows a cubic curve, to have more frequent sparkles when the light has a low battery
    public static readonly HashSet<float> sparkleTimes = new HashSet<float>() { 0, 13, 25, 41, 61, 85, 113, 145, 181, 221, 265, 313, 365, 421, 481, 545, 613, 685, 761, 841, 925, 1013, 1105, 1201, 1301, 1405, 1513, 1625, 1741, 1861 };

    private static Random random = new Random();

    public abstract ItemValue GetLightItemValue(MinEventParams _params);

    public abstract Transform GetLightTransform(MinEventParams _params);

    public virtual void AfterExecute(EntityAlive player) { }

    public override void Execute(MinEventParams _params)
    {
        var player = _params.Self;
        var lightItemValue = GetLightItemValue(_params);
        var lightTransform = GetLightTransform(_params);

        if (player is null || lightItemValue is null || lightItemValue.Activated == 0)
            return;

        float remainingUseTimes = lightItemValue.MaxUseTimes - lightItemValue.UseTimes;

        if (lightTransform != null && sparkleTimes.Contains(remainingUseTimes))
        {
            GameManager.Instance.StartCoroutine(LightSparkleCoroutine(lightTransform, keepActivated: remainingUseTimes > 0));
        }

        if (remainingUseTimes <= 0)
        {
            DeactivateFlashLight(player, lightItemValue);
            return;
        }

        lightItemValue.UseTimes++;

        AfterExecute(player);

        Log.Out($"Decaying {lightItemValue.ItemClass.Name}, usetimes: {lightItemValue.UseTimes}");
    }

    private void DeactivateFlashLight(EntityAlive player, ItemValue itemValue)
    {
        player.MinEventContext.ItemValue = itemValue;
        itemValue.FireEvent(MinEventTypes.onSelfItemDeactivate, player.MinEventContext);
        itemValue.Activated = 0;
    }

    private IEnumerator LightSparkleCoroutine(Transform transform, bool keepActivated)
    {
        float scale = 0.10f;

        for (int i = 0; i < random.Next(2, 6); i++)
        {
            SetLightActive(false, transform);
            yield return new WaitForSeconds((float)random.NextDouble() * scale);

            SetLightActive(true, transform);
            yield return new WaitForSeconds((float)random.NextDouble() * scale);
        }

        SetLightActive(false, transform);
        yield return new WaitForSeconds((float)random.NextDouble() * 2f);

        if (keepActivated)
        {
            SetLightActive(true, transform);
        }

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

}