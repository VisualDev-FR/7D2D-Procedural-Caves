using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WorldGenerationEngineFinal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

using Random = System.Random;
using System.Threading;


public class CavePlanner
{
    private readonly CaveMap cavemap;

    private readonly WorldBuilder worldBuilder;

    private PrefabManager PrefabManager => worldBuilder.PrefabManager;

    private int WorldSize => worldBuilder.WorldSize;

    public readonly string caveTempDir = $"{GameIO.GetUserGameDataDir()}/temp";

    public CavePlanner(WorldBuilder worldBuilder)
    {
        this.worldBuilder = worldBuilder;

        var seed = worldBuilder.Seed + worldBuilder.WorldSize;

        cavemap = new CaveMap();

        CaveConfig.worldSize = WorldSize;
        CaveConfig.rand = new Random(seed);

        worldBuilder.PrefabManager.Clear();
        worldBuilder.PrefabManager.ClearDisplayed();
        worldBuilder.PrefabManager.Cleanup();
    }

    public IEnumerator GenerateCaveMap(CavePrefabManager cavePrefabManager)
    {
        if (worldBuilder.IsCanceled)
            yield break;

        yield return worldBuilder.SetMessage("Spawning cave prefabs...", _logToConsole: true);

        cavePrefabManager.GetUsedCavePrefabs();
        cavePrefabManager.SpawnUnderGroundPrefabs(CaveConfig.TargetPrefabCount);
        cavePrefabManager.SpawnCaveRooms(1000);
        cavePrefabManager.AddSurfacePrefabs();

        var caveGraph = new Graph(CaveCache.cavePrefabManager.Prefabs);

        var threads = new List<Thread>();
        var subLists = CaveUtils.SplitList(caveGraph.Edges.ToList(), 6);
        var localMinimas = new HashSet<CaveBlock>();
        var lockObject = new object();
        int index = 0;

        foreach (var edgeList in subLists)
        {
            var thread = new Thread(() =>
            {
                foreach (var edge in edgeList)
                {
                    string message = $"Cave tunneling: {100f * index++ / caveGraph.Edges.Count:F0}% ({index} / {caveGraph.Edges.Count})";

                    if (worldBuilder.IsCanceled)
                        return;

                    var start = edge.node1;
                    var target = edge.node2;

                    var tunnel = new CaveTunnel(worldBuilder, edge, cavePrefabManager);

                    lock (lockObject)
                    {
                        localMinimas.UnionWith(tunnel.localMinimas);
                        cavemap.AddTunnel(tunnel);
                    }
                }
            })
            {
                Priority = System.Threading.ThreadPriority.Highest
            };

            thread.Start();
            threads.Add(thread);
        }

        while (true)
        {
            bool isThreadAlive = false;
            foreach (var th in threads)
            {
                if (th.IsAlive)
                {
                    isThreadAlive = true;
                    break;
                }
            }

            if (isThreadAlive)
            {
                yield return worldBuilder.SetMessage($"Cave tunneling {100f * cavemap.TunnelsCount / caveGraph.Edges.Count:F0}%");
            }
            else
            {
                break;
            }
        }

        yield return cavemap.SetWaterCoroutine(worldBuilder, localMinimas);

        if (worldBuilder.IsCanceled)
            yield break;

        yield return worldBuilder.SetMessage("Saving cavemap...");

        yield return GenerateCavePreview(cavemap);

        yield return worldBuilder.SetMessage("Creating cave preview...", _logToConsole: true);

        Log.Out($"{cavemap.Count:N0} cave blocks generated");

        yield return null;
    }

    public IEnumerator GenerateCavePreview(CaveMap caveMap)
    {
        Color32 regularPrefabColor = new Color32(255, 255, 255, 32);
        Color32 cavePrefabsColor = new Color32(0, 255, 0, 128);
        Color32 caveEntrancesColor = new Color32(255, 255, 0, 255);
        Color32 caveTunnelColor = new Color32(255, 0, 0, 64);

        var pixels = Enumerable.Repeat(new Color32(0, 0, 0, 255), WorldSize * WorldSize).ToArray();
        var HalfWorldSize = CaveUtils.HalfWorldSize(worldBuilder.WorldSize);

        foreach (PrefabDataInstance pdi in PrefabManager.UsedPrefabsWorld)
        {
            var prefabColor = regularPrefabColor;

            if (pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCaveEntrance))
            {
                prefabColor = caveEntrancesColor;
            }
            else if (pdi.prefab.Tags.Test_AnySet(CaveConfig.tagCave))
            {
                prefabColor = cavePrefabsColor;
            }

            var position = pdi.boundingBoxPosition + HalfWorldSize;
            var size = new Vector3i(pdi.boundingBoxSize);

            if (pdi.rotation == 1 || pdi.rotation == 3)
            {
                size = new Vector3i(pdi.boundingBoxSize.z, pdi.boundingBoxSize.y, pdi.boundingBoxSize.x);
            }

            foreach (var point in CaveUtils.GetBoundingEdges(position, size))
            {
                int index = point.x + point.z * WorldSize;
                pixels[index] = prefabColor;
            }
        }

        var usedTiles = (
            from StreetTile st in worldBuilder.StreetTileMap
            where st.Used
            select st
        ).ToList();

        foreach (var st in usedTiles)
        {
            var position = new Vector3i(st.WorldPosition.x, 0, st.WorldPosition.y);
            var size = new Vector3i(150, 0, 150);

            foreach (var point in CaveUtils.GetBoundingEdges(position, size))
            {
                int index = point.x + point.z * WorldSize;
                pixels[index] = regularPrefabColor;
            }
        }

        foreach (CaveBlock caveblock in caveMap)
        {
            var position = caveblock;
            int index = position.x + position.z * WorldSize;
            try
            {
                caveTunnelColor.a = (byte)position.y;
                pixels[index] = caveTunnelColor;
            }
            catch (IndexOutOfRangeException)
            {
                Log.Error($"[Cave] IndexOutOfRangeException: index={index}, position={caveblock}, worldSize={WorldSize}");
            }
        }

        var image = ImageConversion.EncodeArrayToPNG(pixels, GraphicsFormat.R8G8B8A8_UNorm, (uint)WorldSize, (uint)WorldSize, (uint)WorldSize * 4);
        var filename = $"{caveTempDir}/cavemap.png";

        if (!Directory.Exists(caveTempDir))
            Directory.CreateDirectory(caveTempDir);

        File.WriteAllBytes(filename, image);

        yield return null;
    }

    public void SaveCaveMap()
    {
        cavemap.Save($"{worldBuilder.WorldPath}/cavemap");
    }

}
