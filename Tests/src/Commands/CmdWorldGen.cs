using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;


public class CmdWorldGen : CmdAbstract
{
    private const string worldPath = "ignore/Navezgane";

    private readonly DynamicPrefabDecorator dynamicPrefabDecorator = new DynamicPrefabDecorator();

    public List<PrefabInstance> AllPrefabs => dynamicPrefabDecorator.allPrefabs;

    public override string[] GetCommands()
    {
        return new string[] { "worldgen" };
    }

    public override void Execute(List<string> args)
    {
        var dpd = new DynamicPrefabDecorator();
        // dpd.Load
        // dpd.allPrefabs

        LoadPrefabs(worldPath);

        Console.WriteLine($"prefabs loaded: {AllPrefabs.Count}");
    }

    public void LoadPrefabs(string _path)
    {
        string xmlPath = Path.Combine(_path, "prefabs.xml");

        if (!File.Exists(xmlPath))
        {
            Logging.Warning($"prefab.xml not found at '{_path}'");
            return;
        }

        var document = XDocument.Parse(File.ReadAllText(xmlPath));

        foreach (XElement item in document.XPathSelectElements("//decoration"))
        {
            ParsePrefab(item);
            try
            {
            }
            catch (Exception ex2)
            {
                Log.Error("Loading prefabs xml file for level '" + Path.GetFileName(_path) + "': " + ex2.Message);
            }
        }

        dynamicPrefabDecorator.SortPrefabs();
    }

    private void ParsePrefab(XElement item)
    {
        if (!item.HasAttribute("name"))
            return;

        string name = item.GetAttribute("name");

        Vector3i vector3i = ParseVector(item.GetAttribute("position"));
        StringParsers.TryParseBool(item.GetAttribute("y_is_groundlevel"), out var y_is_groundlevel);

        byte rotation = 0;
        if (item.HasAttribute("rotation"))
        {
            rotation = byte.Parse(item.GetAttribute("rotation"));
        }

        Prefab prefabRotated = dynamicPrefabDecorator.GetPrefabRotated(name, rotation);
        if (prefabRotated == null)
        {
            Log.Warning("Could not load prefab '" + name + "'. Skipping it");
            return;
        }

        if (y_is_groundlevel)
        {
            vector3i.y += prefabRotated.yOffset;
        }

        if (prefabRotated.bTraderArea && SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            dynamicPrefabDecorator.AddTrader(new TraderArea(vector3i, prefabRotated.size, prefabRotated.TraderAreaProtect, prefabRotated.TeleportVolumes));
        }

        PrefabInstance prefabInstance = new PrefabInstance(AllPrefabs.Count, prefabRotated.location, vector3i, rotation, prefabRotated, 0);
        dynamicPrefabDecorator.AddPrefab(prefabInstance, prefabInstance.prefab.HasQuestTag());
    }

    private static Vector3i ParseVector(string str)
    {
        var value = str.Split(',');

        return new Vector3i(
            int.Parse(value[0].Trim()),
            int.Parse(value[1].Trim()),
            int.Parse(value[2].Trim())
        );
    }
}