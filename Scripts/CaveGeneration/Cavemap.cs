using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using WorldGenerationEngineFinal;


public struct CaveBlockLayer
{
    private readonly byte start;

    private readonly byte end;

    private readonly byte rawData;

    public int Count => end - start;

    public CaveBlockLayer(int x, int z, int start, int end, byte rawData)
    {
        this.rawData = rawData;
        this.start = (byte)start;
        this.end = (byte)end;
    }

    public bool IsInside(int y)
    {
        return y >= start && y < end;
    }

    public IEnumerable<CaveBlock> GetBlocks(int x, int z)
    {
        for (int y = start; y <= end; y++)
        {
            yield return new CaveBlock(x, y, z) { rawData = rawData };
        }
    }
}

public class CaveMap
{
    private readonly Dictionary<int, List<CaveBlockLayer>> caveblocks;

    public int BlocksCount => caveblocks.Values.Sum(layer => layer.Count);

    public readonly int worldSize;

    public int TunnelsCount { get; private set; }

    public CaveMap(int worldSize)
    {
        this.worldSize = worldSize;
        caveblocks = new Dictionary<int, List<CaveBlockLayer>>();
    }

    public void AddBlocks(IEnumerable<Vector3i> positions, int rawData)
    {
        throw new NotImplementedException();
    }

    public void AddBlocks(IEnumerable<CaveBlock> _blocks)
    {
        var blockGroup = _blocks.GroupBy(block => block.HashZX());

        foreach (var group in blockGroup)
        {
            int hashZX = group.Key;

            CaveBlock.ZXFromHash(hashZX, out var x, out var z);

            var blocks = group.OrderBy(b => b.y).ToArray();

            if (!caveblocks.ContainsKey(hashZX))
            {
                caveblocks[hashZX] = new List<CaveBlockLayer>();
            }

            byte previousData = blocks[0].rawData;
            int previousY = blocks[0].y;
            int startY = blocks[0].y;

            for (int i = 1; i < blocks.Length; i++)
            {
                int current = blocks[i].y;

                if (current != previousY + 1 || previousData != blocks[i].rawData)
                {
                    caveblocks[hashZX].Add(new CaveBlockLayer(x, z, startY, previousY, previousData));
                    startY = current;
                }

                previousY = current;
                previousData = blocks[i].rawData;
            }

            caveblocks[hashZX].Add(new CaveBlockLayer(x, z, startY, previousY, previousData));
        }
    }

    public void AddTunnel(CaveTunnel tunnel)
    {
        AddBlocks(tunnel.blocks);
        TunnelsCount++;
    }

    public void Save(string dirname, int worldSize)
    {
        throw new NotImplementedException();

        // int regionGridSize = worldSize / CaveConfig.RegionSize;

        // using (var multistream = new MultiStream(dirname, create: true))
        // {
        //     foreach (CaveBlock caveBlock in caveblocks.Values)
        //     {
        //         int region_x = caveBlock.x / CaveConfig.RegionSize;
        //         int region_z = caveBlock.z / CaveConfig.RegionSize;
        //         int regionID = region_x + region_z * regionGridSize;

        //         var writer = multistream.GetWriter($"region_{regionID}.bin");
        //         caveBlock.ToBinaryStream(writer);
        //     }
        // }
    }

    public CaveBlock GetVerticalLowerPoint(CaveBlock start)
    {
        throw new NotImplementedException();

        // var x = start.x;
        // var z = start.z;
        // var y = start.y;

        // int hashcode = CaveBlock.GetHashCode(x, y, z);
        // int offsetHashCode = CaveBlock.GetHashCode(0, -1, 0);

        // while (--y > 0)
        // {
        //     if (!caveblocks.ContainsKey(hashcode + offsetHashCode))
        //     {
        //         return caveblocks[hashcode];
        //     }

        //     hashcode += offsetHashCode;
        // }

        // throw new Exception("Lower point not found");
    }

    private HashSet<int> ExpandWater(CaveBlock waterStart, CavePrefabManager cachedPrefabs)
    {
        throw new NotImplementedException();

        // CaveUtils.Assert(waterStart is CaveBlock, "null water start");

        // var queue = new Queue<int>(1_000);
        // var visited = new HashSet<int>(100_000);
        // var waterHashes = new HashSet<int>(100_000);
        // var startPosition = GetVerticalLowerPoint(waterStart);
        // var start = CaveBlock.GetHashCode(startPosition.x, startPosition.y, startPosition.z);

        // queue.Enqueue(start);

        // while (queue.Count > 0)
        // {
        //     int currentHash = queue.Dequeue();

        //     if (cachedPrefabs.IntersectMarker(caveblocks[currentHash]))
        //         return new HashSet<int>();

        //     if (visited.Contains(currentHash) || !caveblocks.ContainsKey(currentHash))
        //         continue;

        //     visited.Add(currentHash);
        //     waterHashes.Add(currentHash);

        //     foreach (int offsetHash in CaveUtils.offsetHashes)
        //     {
        //         /* NOTE:
        //             f(x, y, z) = Ax + By + z
        //             f(dx, dy, dz) = Adx + Bdy + dz
        //             f(x + dx, y + dy, z + dz)
        //                 = A(x + dx) + B(y + dy) + (z + dz)
        //                 = Ax + Adx + By + Bdy + z + dz
        //                 = (Ax + By + z) + Adx + Bdy + dz
        //                 = f(x, y, z) + f(dx, dy, dz)
        //             => currentHash = f(x, y, z)
        //             => offsetHash = f(dx, dy, dz)
        //             => neighborHash = currentHash + offSetHash
        //                 -> TODO: SIMD Vectorization ?
        //                 -> TODO: f(x, y, z) = (x << 13) + (y << 17) + z
        //         */

        //         var neighborHash = currentHash + offsetHash;

        //         var shouldEnqueue =
        //             caveblocks.ContainsKey(neighborHash)
        //             && !visited.Contains(neighborHash)
        //             && caveblocks[neighborHash].y <= startPosition.y;

        //         if (shouldEnqueue)
        //         {
        //             queue.Enqueue(neighborHash);
        //         }
        //     }
        // }

        // return waterHashes;
    }

    public bool IsCaveAir(int hashcode)
    {
        return caveblocks.ContainsKey(hashcode);
    }

    public IEnumerator SetWaterCoroutine(CavePrefabManager cachedPrefabs, WorldBuilder worldBuilder, HashSet<CaveBlock> localMinimas)
    {
        throw new NotImplementedException();

        // int index = 0;

        // if (!CaveConfig.generateWater)
        //     yield break;

        // foreach (var waterStart in localMinimas)
        // {
        //     index++;

        //     if (worldBuilder.IsCanceled)
        //         yield break;

        //     if (waterStart.isWater)
        //         continue;

        //     HashSet<int> hashcodes = ExpandWater(waterStart, cachedPrefabs);

        //     string message = $"Water processing: {100.0f * index / localMinimas.Count:F0}% ({index} / {localMinimas.Count})";

        //     yield return worldBuilder.SetMessage(message);

        //     foreach (var hashcode in hashcodes)
        //     {
        //         caveblocks[hashcode].isWater = true;
        //     }
        // }
    }

    public IEnumerable<CaveBlock> GetBlocks()
    {
        foreach (var entry in caveblocks)
        {
            int hash = entry.Key;
            CaveBlock.ZXFromHash(hash, out var x, out var z);

            foreach (var layer in entry.Value)
            {
                foreach (var block in layer.GetBlocks(x, z))
                {
                    yield return block;
                }
            }
        }
    }
}