using System;
using System.Collections.Generic;
using System.IO;
using WorldGenerationEngineFinal;
using Path = System.IO.Path;

public class PrefabLoader
{
    private static string userDataPath = @"C:\Users\menan\AppData\Roaming\7DaysToDie";


    public static List<PathAbstractions.AbstractedLocation> GetPrefabPaths(string directory)
    {
        var paths = new List<PathAbstractions.AbstractedLocation>();

        foreach (var filename in Directory.GetFiles(directory))
        {
            string path = Path.Combine(directory, filename);

            if (Path.GetExtension(path) != ".tts")
                continue;

            var loc = new PathAbstractions.AbstractedLocation(
                PathAbstractions.EAbstractedLocationType.UserDataPath,
                Path.GetFileName(path),
                path,
                "",
                false,
                null
            );

            paths.Add(loc);
        }

        foreach (var dir in Directory.GetDirectories(directory))
        {
            paths.AddRange(GetPrefabPaths(dir));
        }

        return paths;
    }

    public static List<PathAbstractions.AbstractedLocation> GetPrefabPaths()
    {
        var prefabPaths = Path.Combine(userDataPath, "LocalPrefabs");
        return GetPrefabPaths(prefabPaths);
    }

    public static Dictionary<string, PrefabData> LoadPrefabs()
    {
        var AllPrefabDatas = new Dictionary<string, PrefabData>();

        List<PathAbstractions.AbstractedLocation> prefabs = GetPrefabPaths();
        FastTags<TagGroup.Poi> filter = FastTags<TagGroup.Poi>.Parse("navonly,devonly,testonly,biomeonly");
        for (int i = 0; i < prefabs.Count; i++)
        {
            PathAbstractions.AbstractedLocation location = prefabs[i];
            int num = location.Folder.LastIndexOf("/Prefabs/");
            if (num >= 0 && location.Folder.Substring(num + 8, 5).EqualsCaseInsensitive("/test"))
            {
                continue;
            }

            if (!File.Exists(location.FullPathNoExtension + ".xml"))
            {
                return null;
            }

            DynamicProperties dynamicProperties = new DynamicProperties();
            if (!dynamicProperties.Load(location.Folder, location.Name, _addClassesToMain: false))
            {
                return null;
            }

            var prefabData = new PrefabData(location, dynamicProperties);

            try
            {
                if (prefabData != null && !prefabData.Tags.Test_AnySet(filter) && !prefabData.Tags.IsEmpty)
                {
                    AllPrefabDatas[location.Name.ToLower()] = prefabData;
                }
            }
            catch (Exception)
            {
                Log.Error("Could not load prefab data for " + location.Name);
            }
        }

        Log.Out($"{PrefabManager.AllPrefabDatas.Count} loaded prefabs");

        return AllPrefabDatas;
    }
}