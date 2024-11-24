using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using WorldGenerationEngineFinal;


public class CaveBlockLayer
{
    public int rawData;

    public byte Start => (byte)((rawData >> 16) & 0xFF);

    public byte End => (byte)((rawData >> 8) & 0xFF);

    public byte BlockRawData => (byte)(rawData & 0xFF);

    public static int Count(int hash)
    {
        var layer = new CaveBlockLayer(hash);
        return layer.End - layer.Start;
    }

    public CaveBlockLayer(int start, int end, byte blockRawData)
    {
        rawData = (start << 16) | (end << 8) | blockRawData;
    }

    public CaveBlockLayer(int bitfield)
    {
        this.rawData = bitfield;
    }

    public static int GetHashCode(int start, int end, byte blockRawData)
    {
        return (start << 16) | (end << 8) | blockRawData;
    }

    public bool IsInside(int y)
    {
        return y >= Start && y < End;
    }

    public IEnumerable<CaveBlock> GetBlocks(int x, int z)
    {
        for (int y = Start; y <= End; y++)
        {
            yield return new CaveBlock(x, y, z) { rawData = BlockRawData };
        }
    }
}

public class CaveMap
{
    private readonly Dictionary<int, List<int>> caveblocks;

    public int BlocksCount => caveblocks.Values.Sum(layers => layers.Sum(layerHash => CaveBlockLayer.Count(layerHash)));

    public readonly int worldSize;

    public int TunnelsCount { get; private set; }

    private readonly object _lock = new object();

    public CaveMap(int worldSize)
    {
        this.worldSize = worldSize;
        caveblocks = new Dictionary<int, List<int>>();
    }

    public void Cleanup()
    {
        foreach (var list in caveblocks.Values)
        {
            list.Clear();
        }

        caveblocks.Clear();
    }

    public void AddBlocks(IEnumerable<Vector3i> positions, byte rawData)
    {
        AddBlocks(positions.Select(pos => new CaveBlock(pos)
        {
            rawData = rawData
        }));
    }

    public void AddBlocks(IEnumerable<CaveBlock> _blocks)
    {
        var blockGroup = _blocks.GroupBy(block => block.HashZX());

        foreach (var group in blockGroup)
        {
            int hashZX = group.Key;

            CaveBlock.ZXFromHash(hashZX, out var x, out var z);

            var blocks = group.OrderBy(b => b.y).ToArray();

            // TODO: better handling of tunnels intersection (implement CaveBlockLayer merging)
            List<int> blockRLE = null;

            lock (_lock)
            {
                if (!caveblocks.ContainsKey(hashZX))
                {
                    caveblocks[hashZX] = new List<int>();
                }
                blockRLE = caveblocks[hashZX];
            }

            byte previousData = blocks[0].rawData;
            int previousY = blocks[0].y;
            int startY = blocks[0].y;

            for (int i = 1; i < blocks.Length; i++)
            {
                int current = blocks[i].y;

                if (current != previousY + 1 || previousData != blocks[i].rawData)
                {
                    lock (_lock)
                    {
                        blockRLE.Add(CaveBlockLayer.GetHashCode(startY, previousY, previousData));
                    }
                    startY = current;
                }

                previousY = current;
                previousData = blocks[i].rawData;
            }

            lock (_lock)
            {
                blockRLE.Add(CaveBlockLayer.GetHashCode(startY, previousY, previousData));
            }
        }
    }

    public void AddTunnel(CaveTunnel tunnel)
    {
        AddBlocks(tunnel.blocks);
        TunnelsCount++;
    }

    public bool ContainsPosition(Vector3i position)
    {
        var hashZX = CaveBlock.HashZX(position.x, position.z);

        if (!caveblocks.ContainsKey(hashZX))
            return false;

        var layer = new CaveBlockLayer(0);

        foreach (var layerHash in caveblocks[hashZX])
        {
            layer.rawData = layerHash;

            if (position.y >= layer.Start && position.y <= layer.End)
            {
                return true;
            }
        }

        return false;
    }

    public void Save(string dirname, int worldSize)
    {
        int regionGridSize = worldSize / CaveConfig.RegionSize;

        using (var multistream = new MultiStream(dirname, create: true))
        {
            foreach (CaveBlock caveBlock in GetBlocks())
            {
                int region_x = caveBlock.x / CaveConfig.RegionSize;
                int region_z = caveBlock.z / CaveConfig.RegionSize;
                int regionID = region_x + region_z * regionGridSize;

                var writer = multistream.GetWriter($"region_{regionID}.bin");
                caveBlock.ToBinaryStream(writer);
            }
        }
    }

    public Vector3i GetVerticalLowerPoint(Vector3i position)
    {
        while (--position.y > 0)
        {
            if (!ContainsPosition(position))
            {
                return position + Vector3i.up;
            }
        }

        throw new Exception("Lower point not found");
    }

    private HashSet<Vector3i> ExpandWater(CaveBlock waterStart, CavePrefabManager cachedPrefabs)
    {
        CaveUtils.Assert(waterStart is CaveBlock, "null water start");

        var queue = new Queue<Vector3i>(1_000);
        var visited = new HashSet<Vector3i>(100_000);
        var waterPositions = new HashSet<Vector3i>(100_000);
        var startPosition = GetVerticalLowerPoint(waterStart.ToVector3i());
        var neighbor = Vector3i.zero;

        queue.Enqueue(startPosition);

        while (queue.Count > 0)
        {
            Vector3i currentPos = queue.Dequeue();

            if (cachedPrefabs.IntersectMarker(currentPos))
                return new HashSet<Vector3i>();

            if (visited.Contains(currentPos) || !ContainsPosition(currentPos))
                continue;

            visited.Add(currentPos);
            waterPositions.Add(currentPos);

            foreach (var offset in CaveUtils.offsets)
            {
                neighbor.x = currentPos.x + offset.x;
                neighbor.y = currentPos.y + offset.y;
                neighbor.z = currentPos.z + offset.z;

                var shouldEnqueue =
                    ContainsPosition(neighbor)
                    && !visited.Contains(neighbor)
                    && neighbor.y <= startPosition.y;

                if (shouldEnqueue)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return waterPositions;
    }

    public IEnumerator SetWaterCoroutine(CavePrefabManager cachedPrefabs, WorldBuilder worldBuilder, HashSet<CaveBlock> localMinimas)
    {
        // if (!CaveConfig.generateWater)
        //     yield break;

        int index = 0;
        var logger = Logging.CreateLogger("Water");

        foreach (var waterStart in localMinimas)
        {
            index++;

            if (worldBuilder.IsCanceled)
                yield break;

            if (waterStart.isWater)
                continue;

            HashSet<Vector3i> positions = ExpandWater(waterStart, cachedPrefabs);

            logger.Debug($"{positions.Count} blocks filled");

            string message = $"Water processing: {100.0f * index / localMinimas.Count:F0}% ({index} / {localMinimas.Count})";

            yield return worldBuilder.SetMessage(message);

            // foreach (var hashcode in positions)
            // {
            //     SetWater(hashcode, true);
            // }
        }
    }

    public IEnumerable<CaveBlock> GetBlocks()
    {
        foreach (var entry in caveblocks)
        {
            int hash = entry.Key;

            CaveBlock.ZXFromHash(hash, out var x, out var z);

            foreach (var layer in entry.Value)
            {
                foreach (var block in new CaveBlockLayer(layer).GetBlocks(x, z))
                {
                    yield return block;
                }
            }
        }
    }
}