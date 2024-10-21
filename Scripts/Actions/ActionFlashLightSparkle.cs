using System.Collections;
using System.Linq;
using GameEvent.SequenceActions;
using UnityEngine;

using Random = System.Random;


// trying to copy TLOU flash light sparkle effect, see https://youtube.com/shorts/fxJuu6odXqY?si=JrFc03VkWKOwozdh
class ActionFlashLightSparkle : ActionBaseItemAction
{
    // values in seconds, which say when the sparkle effect must be played
    // follows a cubic curve, to have more frequent sparkles when the light has a low battery
    private static readonly float[] sparkleTimes = new float[] { 13, 25, 41, 61, 85, 113, 145, 181, 221, 265, 313, 365, 421, 481, 545, 613, 685, 761, 841, 925, 1013, 1105, 1201, 1301, 1405, 1513, 1625, 1741, 1861 };

    private static Random random = new Random();

    public override void OnClientPerform(Entity target)
    {
        var player = target as EntityPlayer;
        var transform = player.inventory.GetHoldingItemTransform();
        var itemValue = player.inventory.holdingItemItemValue;
        var damageRatio = itemValue.UseTimes / itemValue.MaxUseTimes;

        if (transform != null && sparkleTimes.Contains((int)(itemValue.MaxUseTimes - itemValue.UseTimes)))
        {
            Log.Out($"[Cave] sparkle, usetimes: {itemValue.UseTimes}");

            GameManager.Instance.StartCoroutine(LightSparkleCoroutine(transform));
        }
    }

    private IEnumerator LightSparkleCoroutine(Transform transform)
    {
        float scale = 0.25f;

        for (int i = 0; i < random.Next(2, 6); i++)
        {
            SetLightActive(false, transform);
            yield return new WaitForSeconds((float)random.NextDouble() * scale);

            SetLightActive(true, transform);
            yield return new WaitForSeconds((float)random.NextDouble() * scale);
        }

        SetLightActive(false, transform);
        yield return new WaitForSeconds((float)random.NextDouble() * 2f);

        SetLightActive(true, transform);
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
        return new ActionFlashLightSparkle();
    }
}
