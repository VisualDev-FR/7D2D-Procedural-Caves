using System;
using System.Collections.Generic;
using System.Xml.Linq;
using HarmonyLib;


[HarmonyPatch(typeof(LootFromXml), "ParseItemList")]
public static class LootFromXml_ParseItemList
{
    public static bool Prefix(string _containerId, IEnumerable<XElement> _childNodes, List<LootContainer.LootEntry> _itemList, int _minQualityBase, int _maxQualityBase)
    {
        foreach (XElement _childNode in _childNodes)
        {
            LootContainer.LootEntry lootEntry = new LootContainer.LootEntry();
            lootEntry.prob = 1f;
            if (_childNode.HasAttribute("prob") && !StringParsers.TryParseFloat(_childNode.GetAttribute("prob"), out lootEntry.prob))
            {
                throw new Exception("Parsing error prob '" + _childNode.GetAttribute("prob") + "'");
            }
            if (_childNode.HasAttribute("force_prob"))
            {
                StringParsers.TryParseBool(_childNode.GetAttribute("force_prob"), out lootEntry.forceProb);
            }
            if (_childNode.HasAttribute("group"))
            {
                string attribute = _childNode.GetAttribute("group");
                if (!LootContainer.lootGroups.TryGetValue(attribute, out lootEntry.group))
                {
                    // PATCH IS HERE
                    Logging.Warning("lootgroup '" + attribute + "' does not exist or has not been defined before being reference by lootcontainer/lootgroup name='" + _containerId + "'");
                    continue;
                    // PATCH IS HERE
                }
            }
            else
            {
                if (!_childNode.HasAttribute("name"))
                {
                    throw new Exception("Attribute 'name' or 'group' missing on item in lootcontainer/lootgroup name='" + _containerId + "'");
                }
                lootEntry.item = new LootContainer.LootItem();
                string attribute2 = _childNode.GetAttribute("name");
                lootEntry.item.itemValue = ItemClass.GetItem(attribute2);
                if (lootEntry.item.itemValue.IsEmpty())
                {
                    throw new Exception("Item with name '" + attribute2 + "' not found!");
                }
            }
            string attribute3 = _childNode.GetAttribute("tags");
            if (attribute3.Length > 0)
            {
                lootEntry.tags = FastTags<TagGroup.Global>.Parse(attribute3);
            }
            lootEntry.minCount = 1;
            lootEntry.maxCount = 1;
            if ((lootEntry.item == null || ItemClass.GetForId(lootEntry.item.itemValue.type).CanStack()) && _childNode.HasAttribute("count"))
            {
                StringParsers.ParseMinMaxCount(_childNode.GetAttribute("count"), out lootEntry.minCount, out lootEntry.maxCount);
            }
            lootEntry.minQuality = _minQualityBase;
            lootEntry.maxQuality = _maxQualityBase;
            if (_childNode.HasAttribute("quality"))
            {
                StringParsers.ParseMinMaxCount(_childNode.GetAttribute("quality"), out lootEntry.minQuality, out lootEntry.maxQuality);
            }
            if (_childNode.HasAttribute("loot_prob_template"))
            {
                lootEntry.lootProbTemplate = _childNode.GetAttribute("loot_prob_template");
            }
            else
            {
                lootEntry.lootProbTemplate = string.Empty;
            }
            if (_childNode.HasAttribute("mods"))
            {
                lootEntry.modsToInstall = _childNode.GetAttribute("mods").Split(',');
            }
            else
            {
                lootEntry.modsToInstall = new string[0];
            }
            if (_childNode.HasAttribute("mod_chance"))
            {
                lootEntry.modChance = StringParsers.ParseFloat(_childNode.GetAttribute("mod_chance"));
            }
            if (_childNode.HasAttribute("loot_stage_count_mod"))
            {
                lootEntry.lootstageCountMod = StringParsers.ParseFloat(_childNode.GetAttribute("loot_stage_count_mod"));
            }
            _itemList.Add(lootEntry);
        }

        return false;
    }
}



