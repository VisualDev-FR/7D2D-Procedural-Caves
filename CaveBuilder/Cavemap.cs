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

    public sbyte GetDensity(Vector3i pos)
    {
        var hashcode = CaveBlock.GetHashCode(pos.x, pos.y, pos.z);
        return GetDensity(hashcode);
    }

    public sbyte GetDensity(int hashcode)
    {
        if (caveblocks.TryGetValue(hashcode, out var block))
        {
            return block.density;
        }

        return MarchingCubes.DensityTerrain;
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

        int hashcode = CaveBlock.GetHashCode(x, y, z);
        int offsetHashCode = CaveBlock.GetHashCode(0, -1, 0);

        while (--y > 0)
        {
            if (!caveblocks.ContainsKey(hashcode + offsetHashCode))
            {
                return caveblocks[hashcode];
            }

            hashcode += offsetHashCode;
        }

        throw new Exception("Lower point not found");
    }

    private HashSet<int> ExpandWater(CaveBlock waterStart, PrefabCache cachedPrefabs)
    {
        CaveUtils.Assert(waterStart is CaveBlock, "null water start");

        var queue = new Queue<int>(1_000);
        var visited = new HashSet<int>(100_000);
        var waterHashes = new HashSet<int>(100_000);
        var startPosition = GetVerticalLowerPoint(waterStart);
        var start = CaveBlock.GetHashCode(startPosition.x, startPosition.y, startPosition.z);

        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            int currentHash = queue.Dequeue();

            if (cachedPrefabs.IntersectMarker(caveblocks[currentHash]))
                return new HashSet<int>();

            if (visited.Contains(currentHash) || !caveblocks.ContainsKey(currentHash))
                continue;

            visited.Add(currentHash);
            waterHashes.Add(currentHash.GetHashCode());

            foreach (int offsetHash in CaveUtils.neighborsHashes)
            {
                /* NOTE:
                    f(x, y, z) = Ax + By + z
                    f(dx, dy, dz) = Adx + Bdy + dz
                    f(x + dx, y + dy, z + dz)
                        = A(x + dx) + B(y + dy) + (z + dz)
                        = Ax + Adx + By + Bdy + z + dz
                        = (Ax + By + z) + Adx + Bdy + dz
                        = f(x, y, z) + f(dx, dy, dz)
                    => currentHash = f(x, y, z)
                    => offsetHash = f(dx, dy, dz)
                    => neighborHash = currentHash + offSetHash
                        -> TODO: SIMD Vectorization ?
                        -> TODO: f(x, y, z) = (x << 13) + (y << 17) + z
                */

                var neighborHash = currentHash + offsetHash;
                var shouldEnqueue =
                    caveblocks[neighborHash].y <= startPosition.y
                    && !visited.Contains(neighborHash)
                    && caveblocks.ContainsKey(neighborHash);

                if (shouldEnqueue)
                {
                    queue.Enqueue(neighborHash);
                }
            }
        }

        return waterHashes;
    }

    public void SetWater(HashSet<CaveBlock> localMinimas, PrefabCache cachedPrefabs)
    {
        if (!CaveConfig.generateWater)
            return;

        int index = 0;

        // TODO: multi-thread this loop
        foreach (var waterStart in localMinimas)
        {
            index++;

            if (waterStart.isWater)
                continue;

            HashSet<int> hashcodes = ExpandWater(waterStart, cachedPrefabs);

            Log.Out($"Water processing: {100.0f * index / localMinimas.Count:F0}% ({index} / {localMinimas.Count}) {hashcodes.Count:N0}");

            foreach (var hashcode in hashcodes)
            {
                caveblocks[hashcode].isWater = true;
            }
        }
    }

    public bool IsCaveAir(int hashcode)
    {
        return caveblocks.ContainsKey(hashcode);
    }

    public IEnumerator SetWaterCoroutine(HashSet<CaveBlock> localMinimas, PrefabCache cachedPrefabs)
    {
        int index = 0;

        if (!CaveConfig.generateWater)
            yield break;

        foreach (var waterStart in localMinimas)
        {
            index++;

            if (WorldBuilder.Instance.IsCanceled)
                yield break;

            if (waterStart.isWater)
                continue;

            HashSet<int> hashcodes = ExpandWater(waterStart, cachedPrefabs);

            string message = $"Water processing: {100.0f * index / localMinimas.Count:F0}% ({index} / {localMinimas.Count})";

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