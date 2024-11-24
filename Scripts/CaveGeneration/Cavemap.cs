using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using WorldGenerationEngineFinal;


public struct LayerRLE
{
    public int rawData;

    public byte Start
    {
        get => (byte)((rawData >> 16) & 0xFF);
        set => rawData = (rawData & ~0x00FF0000) | (value << 16);
    }

    public byte End
    {
        get => (byte)((rawData >> 8) & 0xFF);
        set => rawData = (rawData & ~0x0000FF00) | (value << 8);
    }

    public byte BlockRawData
    {
        get => (byte)(rawData & 0xFF);
        set => rawData = (rawData & ~0x000000FF) | value;
    }

    public static int Count(int hash)
    {
        var layer = new LayerRLE(hash);
        return layer.End - layer.Start;
    }

    public LayerRLE(int start, int end, byte blockRawData)
    {
        rawData = (start << 16) | (end << 8) | blockRawData;
    }

    public LayerRLE(IEnumerable<Vector3i> positions, byte blockRawData)
    {
        var start = int.MaxValue;
        var end = int.MinValue;

        foreach (var pos in positions)
        {
            start = Utils.FastMin(start, pos.y - 1);
            end = Utils.FastMax(end, pos.y + 1);
        }

        rawData = (start << 16) | (end << 8) | blockRawData;
    }

    public LayerRLE(int bitfield)
    {
        this.rawData = bitfield;
    }

    public static int GetHashCode(int start, int end, byte blockRawData)
    {
        return (start << 16) | (end << 8) | blockRawData;
    }

    public bool Contains(int y)
    {
        return y >= Start && y < End;
    }

    public IEnumerable<CaveBlock> GetBlocks(int x, int z)
    {
        for (int y = Start; y < End; y++)
        {
            yield return new CaveBlock(x, y, z) { rawData = BlockRawData };
        }
    }

    public void SetWater(bool value)
    {
        BlockRawData = (byte)(value ? (BlockRawData | 0b0000_0001) : (BlockRawData & 0b1111_1110));
    }

    public bool IsWater()
    {
        return (BlockRawData & 0b0000_0001) != 0;
    }

}

public class CaveMap
{
    private readonly Dictionary<int, List<int>> caveblocks;

    public int BlocksCount => caveblocks.Values.Sum(layers => layers.Sum(layerHash => LayerRLE.Count(layerHash)));

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

            if (!caveblocks.ContainsKey(hashZX))
            {
                lock (_lock)
                {
                    caveblocks[hashZX] = new List<int>();
                }
            }

            foreach (var layerHash in RLECompress(group))
            {
                lock (_lock)
                {
                    caveblocks[hashZX].Add(layerHash);
                }
            }
        }
    }

    public void AddTunnel(CaveTunnel tunnel)
    {
        AddBlocks(tunnel.blocks);
        TunnelsCount++;
    }

    public IEnumerable<int> RLECompress(IEnumerable<CaveBlock> caveBlocks)
    {
        var blocks = caveBlocks.OrderBy(b => b.y).ToArray();

        byte previousData = blocks[0].rawData;
        int previousY = blocks[0].y;
        int startY = blocks[0].y;

        for (int i = 1; i < blocks.Length; i++)
        {
            int current = blocks[i].y;

            if (current != previousY + 1 || previousData != blocks[i].rawData)
            {
                yield return LayerRLE.GetHashCode(startY, previousY, previousData);
                startY = current;
            }

            previousY = current;
            previousData = blocks[i].rawData;
        }

        yield return LayerRLE.GetHashCode(startY, previousY, previousData);
    }

    public bool IsCave(Vector3i position)
    {
        var hashZX = CaveBlock.HashZX(position.x, position.z);

        if (!caveblocks.ContainsKey(hashZX))
            return false;

        var layer = new LayerRLE(0);

        foreach (var layerHash in caveblocks[hashZX])
        {
            layer.rawData = layerHash;

            if (layer.Contains(position.y))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsWater(Vector3i position)
    {
        var hashZX = CaveBlock.HashZX(position.x, position.z);

        if (!caveblocks.ContainsKey(hashZX))
            return false;

        var layer = new LayerRLE(0);

        foreach (var layerHash in caveblocks[hashZX])
        {
            layer.rawData = layerHash;

            if (layer.Contains(position.y))
            {
                return layer.IsWater();
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
        while (position.y-- > 0)
        {
            if (!IsCave(position))
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

            if (visited.Contains(currentPos) || !IsCave(currentPos))
                continue;

            visited.Add(currentPos);
            waterPositions.Add(currentPos);

            foreach (var offset in CaveUtils.offsets)
            {
                neighbor.x = currentPos.x + offset.x;
                neighbor.y = currentPos.y + offset.y;
                neighbor.z = currentPos.z + offset.z;

                var shouldEnqueue =
                    IsCave(neighbor)
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

        CaveUtils.Assert(new CaveBlock() { rawData = 1 }.isWater, "");

        foreach (var waterStart in localMinimas)
        {
            index++;

            if (worldBuilder.IsCanceled)
                yield break;

            if (IsWater(waterStart.ToVector3i()))
                continue;

            HashSet<Vector3i> positions = ExpandWater(waterStart, cachedPrefabs);

            string message = $"Water processing: {100.0f * index / localMinimas.Count:F0}% ({index} / {localMinimas.Count})";

            yield return worldBuilder.SetMessage(message);

            if (positions.Count > 0)
            {
                SetWater(positions);
            }
        }
    }

    public HashSet<Vector3i> SetWater(CavePrefabManager cachedPrefabs, HashSet<CaveBlock> localMinimas)
    {
        // if (!CaveConfig.generateWater)
        //     yield break;

        int index = 0;
        var result = new HashSet<Vector3i>();

        foreach (var waterStart in localMinimas)
        {
            index++;

            if (IsWater(waterStart.ToVector3i()))
                continue;

            var positions = ExpandWater(waterStart, cachedPrefabs);

            result.UnionWith(positions);

            if (positions.Count > 0)
            {
                SetWater(positions);
            }
        }

        return result;
    }

    private void SetWater(HashSet<Vector3i> positions)
    {
        var layer = new LayerRLE(0);

        // public bool IsInside(int y)
        // {
        //     return y >= Start && y < End;
        // }

        foreach (var group in positions.GroupBy(p => CaveBlock.HashZX(p.x, p.z)))
        {
            var hashcode = group.Key;
            var waterLayer = new LayerRLE(group, 1);
            var layers = caveblocks[hashcode].ToList();

            for (int i = layers.Count - 1; i >= 0; i--)
            {
                layer.rawData = layers[i];

                waterLayer.BlockRawData = layer.BlockRawData;
                waterLayer.SetWater(true);

                if (waterLayer.Start <= layer.Start && waterLayer.End >= layer.End)
                {
                    caveblocks[hashcode][i] = LayerRLE.GetHashCode(
                        layer.Start,
                        layer.End,
                        waterLayer.BlockRawData
                    );
                }
                else if (waterLayer.Start <= layer.Start && waterLayer.End >= layer.Start && waterLayer.End < layer.End)
                {
                    caveblocks[hashcode].RemoveAt(i);

                    caveblocks[hashcode].Add(LayerRLE.GetHashCode(
                        layer.Start,
                        waterLayer.End,
                        waterLayer.BlockRawData
                    ));

                    caveblocks[hashcode].Add(LayerRLE.GetHashCode(
                        waterLayer.End,
                        layer.End,
                        layer.BlockRawData
                    ));
                }
            }
        }
    }

    public IEnumerable<CaveBlock> GetBlocks()
    {
        foreach (var entry in caveblocks)
        {
            int hash = entry.Key;

            CaveBlock.ZXFromHash(hash, out var x, out var z);

            foreach (var hashcode in entry.Value)
            {
                var layer = new LayerRLE(hashcode);

                // Logging.Debug(layer.Start, layer.End, layer.End - layer.Start);

                CaveUtils.Assert(layer.Start <= layer.End, "");

                foreach (var block in layer.GetBlocks(x, z))
                {
                    yield return block;
                }
            }
        }
    }
}