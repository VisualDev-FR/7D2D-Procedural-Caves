using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

public class PrefabLoader
{
    public readonly List<PrefabDataInstance> allPrefabs = new List<PrefabDataInstance>();

    public IEnumerator LoadPrefabs(WorldDatas worldDatas)
    {
        var xmlPath = worldDatas.prefabsPath;

        if (!File.Exists(xmlPath))
        {
            Logging.Warning($"prefab.xml not found: '{xmlPath}'");
            yield break;
        }

        var document = XDocument.Parse(File.ReadAllText(xmlPath));

        foreach (XElement item in document.XPathSelectElements("//decoration"))
        {
            yield return AddPrefab(item);
        }
    }

    private IEnumerator AddPrefab(XElement item)
    {
        if (!item.HasAttribute("name"))
            yield break;

        string prefabName = item.GetAttribute("name");

        Vector3i position = Vector3i.Parse(item.GetAttribute("position"));

        byte rotation = 0;
        if (item.HasAttribute("rotation"))
        {
            rotation = byte.Parse(item.GetAttribute("rotation"));
        }

        var location = PathAbstractions.PrefabsSearchPaths.GetLocation(prefabName);
        var prefabData = PrefabData.LoadPrefabData(location);
        var pdi = new PrefabDataInstance(allPrefabs.Count, position, rotation, prefabData);

        allPrefabs.Add(pdi);

        yield return null;
    }

}