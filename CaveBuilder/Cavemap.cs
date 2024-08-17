using System;
using System.Collections;
using System.Collections.Generic;
using WorldGenerationEngineFinal;

public class CaveMap : IEnumerable<CaveBlock>
{
    private readonly Dictionary<int, CaveBlock> caveblocks;

    public int Count => caveblocks.Count;

    public CaveMap()
    {
        caveblocks = new Dictionary<int, CaveBlock>();
    }

    public bool TryGetValue(int hashcode, out CaveBlock block)
    {
        return caveblocks.TryGetValue(hashcode, out block);
    }

    public bool Contains(CaveBlock block)
    {
        return caveblocks.ContainsKey(block.GetHashCode());
    }

    public bool Contains(int hashcode)
    {
        return caveblocks.ContainsKey(hashcode);
    }

    public bool Contains(int x, int y, int z)
    {
        return caveblocks.ContainsKey(CaveBlock.GetHashCode(x, y, z));
    }

    public CaveBlock GetBlock(int hashcode)
    {
        return caveblocks[hashcode];
    }

    public CaveBlock GetBlock(int x, int y, int z)
    {
        return caveblocks[CaveBlock.GetHashCode(x, y, z)];
    }

    public void UnionWith(HashSet<CaveBlock> others)
    {
        foreach (var block in others)
        {
            caveblocks[block.GetHashCode()] = block;
        }
    }

    public void Save(string dirname)
    {
        using (var multistream = new MultiStream(dirname, create: true))
        {
            foreach (CaveBlock caveBlock in caveblocks.Values)
            {
                var position = caveBlock.position;

                int region_x = position.x / CaveBuilder.RegionSize;
                int region_z = position.z / CaveBuilder.RegionSize;
                int regionID = region_x + region_z * CaveBuilder.regionGridSize;

                var writer = multistream.GetWriter($"region_{regionID}.bin");
                caveBlock.ToBinaryStream(writer);
            }
        }
    }

    public CaveBlock GetVerticalLowerPoint(CaveBlock start)
    {
        var x = start.x;
        var z = start.z;
        var y = start.y;
        var index = 256;

        while (true && --index > 0)
        {
            int hashcode = CaveBlock.GetHashCode(x, y - 1, z);

            if (!caveblocks.ContainsKey(hashcode))
            {
                return GetBlock(CaveBlock.GetHashCode(x, y, z));
            }

            y--;
        }

        throw new Exception("Lower point not found");
    }

    private HashSet<int> ExpandWater(CaveBlock waterStart, PrefabCache cachedPrefabs)
    {
        CaveUtils.Assert(waterStart is CaveBlock, "null water start");

        var queue = new Queue<CaveBlock>();
        var visited = new HashSet<CaveBlock>();
        var waterBlocks = new HashSet<int>();
        var startPosition = GetVerticalLowerPoint(waterStart);
        var start = GetBlock(startPosition.x, startPosition.y, startPosition.z);

        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            CaveBlock block = queue.Dequeue();

            if (cachedPrefabs.IntersectMarker(block))
                return new HashSet<int>();

            if (visited.Contains(block) || !Contains(block))
                continue;

            visited.Add(block);
            waterBlocks.Add(block.GetHashCode());

            foreach (var offset in CaveUtils.neighborsOffsets)
            {
                var hashcode = CaveBlock.GetHashCode(
                    block.x + offset.x,
                    block.y + offset.y,
                    block.z + offset.z
                );

                if (!TryGetValue(hashcode, out var neighbor))
                    continue;

                if (!visited.Contains(neighbor) && neighbor.y <= start.y)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return waterBlocks;
    }

    public void SetWater(HashSet<CaveBlock> localMinimas, PrefabCache cachedPrefabs)
    {
        if (!CaveConfig.generateWater)
            return;

        int index = 0;

        // TODO: multi-thread this loop
        foreach (var waterStart in localMinimas)
        {
            if (waterStart.isWater)
                continue;

            HashSet<int> hashcodes = ExpandWater(waterStart, cachedPrefabs);

            Log.Out($"Water processing: {100.0f * ++index / localMinimas.Count:F0}% ({index} / {localMinimas.Count}) {hashcodes.Count:N0}");

            foreach (var hashcode in hashcodes)
            {
                caveblocks[hashcode].isWater = true;
            }
        }
    }

    public bool IsCave(int x, int y, int z)
    {
        return caveblocks.ContainsKey(CaveBlock.GetHashCode(x, y, z));
    }

    public IEnumerator SetWaterCoroutine(HashSet<CaveBlock> localMinimas, PrefabCache cachedPrefabs)
    {
        int index = 0;

        if (!CaveConfig.generateWater)
            yield break;

        foreach (var waterStart in localMinimas)
        {
            if (WorldBuilder.Instance.IsCanceled)
                yield break;

            if (waterStart.isWater)
                continue;

            HashSet<int> hashcodes = ExpandWater(waterStart, cachedPrefabs);

            string message = $"Water processing: {100.0f * ++index / localMinimas.Count:F0}% ({index} / {localMinimas.Count})";

            yield return WorldBuilder.Instance.SetMessage(message);

            foreach (var hashcode in hashcodes)
            {
                caveblocks[hashcode].isWater = true;
            }
        }
    }

    public IEnumerator<CaveBlock> GetEnumerator()
    {
        return caveblocks.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}