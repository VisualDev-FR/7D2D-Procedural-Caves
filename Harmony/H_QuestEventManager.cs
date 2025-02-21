using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(QuestEventManager), "SetupTraderPrefabList")]
public static class QuestEventManager_SetupTraderPrefabList
{
    public static bool Prefix(QuestEventManager __instance, TraderArea area)
    {
        var logger = Logging.CreateLogger("TheDescent.H_QuestEventManager");

        if (__instance.TraderPrefabList.ContainsKey(area))
        {
            return false;
        }

        Vector3 a = area.Position.ToVector3();
        List<PrefabInstance> pOIPrefabs = GameManager.Instance.GetDynamicPrefabDecorator().GetPOIPrefabs();
        List<QuestEventManager.PrefabListData> list = new List<QuestEventManager.PrefabListData>();

        var prefabListData = new QuestEventManager.PrefabListData();
        var prefabListData2 = new QuestEventManager.PrefabListData();
        var prefabListData3 = new QuestEventManager.PrefabListData();

        list.Add(prefabListData);
        list.Add(prefabListData2);
        list.Add(prefabListData3);

        for (int i = 0; i < pOIPrefabs.Count; i++)
        {
            var prefabInstance = pOIPrefabs[i];
            var distance = Vector3.Distance(a, prefabInstance.boundingBoxPosition);

            if (prefabInstance.prefab.tags.Test_AnySet(CaveTags.tagUnderground))
            {
                logger.Warning($"Skip quest for underground poi: '{prefabInstance.name}'");
                continue;
            }

            if (distance <= 500f)
            {
                prefabListData.AddPOI(prefabInstance);
            }
            else if (distance <= 1500f)
            {
                prefabListData2.AddPOI(prefabInstance);
            }
            else
            {
                prefabListData3.AddPOI(prefabInstance);
            }
        }

        __instance.TraderPrefabList.Add(area, list);

        return false;
    }
}