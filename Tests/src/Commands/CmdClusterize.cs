using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

public class CmdClusterize : CmdAbstract
{
    private static readonly string sevenDaysDir = "C:/SteamLibrary/steamapps/common/7 Days To Die";

    private static readonly string localPrefabs = Environment.GetEnvironmentVariable("appdata") + "/7DaysToDie";

    public override string[] GetCommands()
    {
        return new string[] { "cluster" };
    }

    public override void Execute(List<string> args)
    {
        var timer = ProfilingUtils.StartTimer();
        var ttsFiles = GetTTSFiles();

        foreach (var path in ttsFiles)
        {
            ClusterizePrefab(path);
        }

        Console.WriteLine($"files={ttsFiles.Count()}, timer={timer.ElapsedMilliseconds}ms");
    }

    public IEnumerable<string> GetTTSFiles()
    {
        var sdaysFiles = Directory.EnumerateFiles(sevenDaysDir, "*.tts", SearchOption.AllDirectories);
        var localFiles = Directory.EnumerateFiles(localPrefabs, "*.tts", SearchOption.AllDirectories);

        return sdaysFiles.Concat(localFiles);
    }

    public void ClusterizePrefab(string ttsPath)
    {
        var prefabName = Path.GetFileNameWithoutExtension(ttsPath);
        var directory = Path.GetDirectoryName(ttsPath);
        var xmlPath = Path.GetFullPath($"{directory}/{prefabName}.xml");

        var yOffset = GetYOffset(xmlPath);
        var clusters = BlockClusterizer.Clusterize(ttsPath, yOffset);

        Logging.Info($"{Path.GetFileName(ttsPath)}: yOffset={yOffset}, {clusters.Count} clusters found");
    }

    public string ComputeMD5Hash(string ttsPath)
    {
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            byte[] inputBytes = File.ReadAllBytes(ttsPath);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "");
        }
    }

    public int GetYOffset(string xmlPath)
    {
        var fileContent = File.ReadAllText(xmlPath);
        var document = new XmlDocument();
        document.LoadXml(fileContent);

        var node = document.SelectSingleNode("//property[@name='YOffset']");

        if (node != null)
        {
            return int.Parse(node.Attributes["value"].Value);
        }

        return 0;
    }

    public void GeneratePreview(string ttsPath, int yOffset, List<BoundingBox> clusters)
    {
        var prefabVoxels = TTSReader.ReadUndergroundBlocks(ttsPath, yOffset).Select(pos => new Voxell(pos))
            .ToHashSet();

        DrawingUtils.GenerateObjFile("ignore/prefab.obj", prefabVoxels, false);

        var clusterVoxels = clusters
            .Select(cluster => new Voxell(cluster.start, cluster.size, WaveFrontMaterial.DarkGreen) { force = true })
            .ToHashSet();

        DrawingUtils.GenerateObjFile("ignore/clusters.obj", clusterVoxels, false);
    }

}