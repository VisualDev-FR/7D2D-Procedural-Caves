using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Generic;

public static class PrefabLoader
{
    public static IEnumerable<PrefabDataInstance> LoadPrefabs(string xmlPath)
    {
        if (!File.Exists(xmlPath))
        {
            Logging.Warning($"prefab.xml not found: '{xmlPath}'");
        }

        var document = XDocument.Parse(File.ReadAllText(xmlPath));
        var prefabID = 0;

        foreach (XElement item in document.XPathSelectElements("//decoration"))
        {
            if (LoadPrefab(item, prefabID) is PrefabDataInstance pdi)
            {
                yield return pdi;
                prefabID++;
            }
        }
    }

    private static PrefabDataInstance LoadPrefab(XElement item, int id)
    {
        if (!item.HasAttribute("name"))
            return null;

        string prefabName = item.GetAttribute("name");

        Vector3i position = Vector3i.Parse(item.GetAttribute("position"));

        byte rotation = 0;
        if (item.HasAttribute("rotation"))
        {
            rotation = byte.Parse(item.GetAttribute("rotation"));
        }

        var location = PathAbstractions.PrefabsSearchPaths.GetLocation(prefabName);
        var prefabData = PrefabData.LoadPrefabData(location);

        return new PrefabDataInstance(id, position, rotation, prefabData);
    }

}