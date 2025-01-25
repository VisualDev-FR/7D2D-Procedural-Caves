using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using WorldGenerationEngineFinal;


public struct RLELayer
{
    public int rawData;

    public byte Start
    {
        get => Bitfield.GetByte(rawData, 16);
        set => Bitfield.SetByte(ref rawData, value, 16);
    }

    public byte End
    {
        get => Bitfield.GetByte(rawData, 8);
        set => Bitfield.SetByte(ref rawData, value, 8);
    }

    public byte BlockRawData
    {
        get => Bitfield.GetByte(rawData, 0);
        set => Bitfield.SetByte(ref rawData, value, 0);
    }

    public int Size => End - Start;

    public static int Count(int hash)
    {
        var layer = new RLELayer(hash);
        return layer.End - layer.Start;
    }

    public RLELayer(int start, int end, byte blockRawData)
    {
        rawData = (start << 16) | (end << 8) | blockRawData;
    }

    public RLELayer(IEnumerable<Vector3i> positions, byte blockRawData)
    {
        var start = int.MaxValue;
        var end = int.MinValue;

        foreach (var pos in positions)
        {
            start = Utils.FastMin(start, pos.y);
            end = Utils.FastMax(end, pos.y);
        }

        rawData = (start << 16) | (end << 8) | blockRawData;
    }

    public RLELayer(int bitfield)
    {
        this.rawData = bitfield;
    }

    public static int GetHashCode(int start, int end, byte blockRawData)
    {
        return (start << 16) | (end << 8) | blockRawData;
    }

    public bool Contains(int y)
    {
        return y >= Start && y <= End;
    }

    public IEnumerable<CaveBlock> GetBlocks(int x, int z)
    {
        for (int y = Start; y <= End; y++)
        {
            yield return new CaveBlock(x, y, z) { rawData = BlockRawData };
        }
    }

    public bool IsWater()
    {
        return (BlockRawData & 0b0000_0001) != 0;
    }

    public void SetWater(bool value)
    {
        BlockRawData = (byte)(value ? (BlockRawData | 0b0000_0001) : (BlockRawData & 0b1111_1110));
    }

    public void SetRope(bool value)
    {
        BlockRawData = (byte)(value ? (BlockRawData | 0b0000_1000) : (BlockRawData & 0b1111_0111));
    }

    public override int GetHashCode()
    {
        return rawData;
    }

    public static List<int> CompressLayers(IEnumerable<int> layers)
    {
        var layer = new RLELayer();
        var values = new byte?[256];
        byte minSet = 255;
        byte maxSet = 0;

        foreach (var layerHash in layers)
        {
            layer.rawData = layerHash;

            minSet = minSet < layer.Start ? minSet : layer.Start;
            maxSet = maxSet > layer.End ? maxSet : layer.End;

            for (int i = layer.Start; i <= layer.End; i++)
            {
                if (values[i] is null)
                {
                    values[i] = layer.BlockRawData;
                }
                else if (values[i].Value != layer.BlockRawData)
                {
                    values[i] = (byte)(values[i].Value | layer.BlockRawData);
                }
            }
        }

        var result = new List<int>();

        byte? previousData = values[minSet].Value;
        int startY = minSet;

        for (int i = minSet + 1; i <= maxSet; i++)
        {
            if (values[i] != previousData)
            {
                if (previousData.HasValue)
                {
                    result.Add(GetHashCode(startY, i - 1, previousData.Value));
                }

                startY = i;
                previousData = values[i];
            }
        }

        if (previousData.HasValue)
        {
            result.Add(GetHashCode(startY, maxSet, previousData.Value));
        }

        return result;
    }

    public bool AssertEquals(byte start, byte end, byte data)
    {
        CaveUtils.Assert(this.Start == start, $"start, value: '{this.Start}', expected: '{start}'");
        CaveUtils.Assert(this.End == end, $"end, value: '{this.End}', expected: '{end}'");
        CaveUtils.Assert(this.BlockRawData == data, $"data, value: '{this.BlockRawData}', expected: '{data}'");

        return true;
    }

}

public class CaveMap
{
    private readonly Dictionary<int, List<int>> caveblocks;

    public int BlocksCount => caveblocks.Values.Sum(layers => layers.Sum(layerHash => RLELayer.Count(layerHash)));

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

            foreach (var layerHash in RLEEncode(group))
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
        TunnelsCount++;

        foreach (var entry in tunnel.layers)
        {
            var zxHash = entry.Key;
            var layerHashes = entry.Value;

            if (!caveblocks.ContainsKey(zxHash))
            {
                lock (_lock)
                {
                    caveblocks[zxHash] = layerHashes;
                }
                continue;
            }
            else
            {
                lock (_lock)
                {
                    caveblocks[zxHash].AddRange(layerHashes);
                }
            }

            lock (_lock)
            {
                caveblocks[zxHash] = RLELayer.CompressLayers(caveblocks[zxHash]);
            }
        }
    }

    public IEnumerable<int> RLEEncode(IEnumerable<CaveBlock> caveBlocks)
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
                yield return RLELayer.GetHashCode(startY, previousY, previousData);
                startY = current;
            }

            previousY = current;
            previousData = blocks[i].rawData;
        }

        yield return RLELayer.GetHashCode(startY, previousY, previousData);
    }

    public bool IsCave(Vector3i position)
    {
        var hashZX = CaveBlock.HashZX(position.x, position.z);

        if (!caveblocks.ContainsKey(hashZX))
            return false;

        var layer = new RLELayer(0);

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

        var layer = new RLELayer(0);

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
            foreach (var entry in caveblocks)
            {
                CaveBlock.ZXFromHash(entry.Key, out var x, out var z);

                int region_x = x >> CaveConfig.RegionSizeOffset;
                int region_z = z >> CaveConfig.RegionSizeOffset;
                int regionID = region_x + region_z * regionGridSize;

                var writer = multistream.GetWriter($"region_{regionID}.bin");

                writer.Write(x);
                writer.Write(z);
                writer.Write(entry.Value.Count);

                foreach (var layerHash in entry.Value)
                {
                    writer.Write(layerHash);
                }
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

    private HashSet<Vector3i> ExpandWater(Vector3i waterStart, CavePrefabManager cachedPrefabs)
    {
        var queue = new Queue<Vector3i>(1_000);
        var visited = new HashSet<Vector3i>(100_000);
        var waterPositions = new HashSet<Vector3i>(100_000);
        var startPosition = GetVerticalLowerPoint(waterStart);
        var neighbor = Vector3i.zero;

        int maxWaterDepth = int.MaxValue;

        queue.Enqueue(startPosition);

        while (queue.Count > 0)
        {
            Vector3i currentPos = queue.Dequeue();

            if (cachedPrefabs.IntersectMarker(currentPos) || (startPosition.y - currentPos.y) >= maxWaterDepth)
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

    public IEnumerator SetWaterCoroutine(CavePrefabManager cachedPrefabs, WorldBuilder worldBuilder, HashSet<Vector3i> localMinimas)
    {
        if (CaveConfig.caveWater == WorldBuilder.GenerationSelections.None)
        {
            yield break;
        }

        int index = 0;
        int count = 0;

        var waterNoise = new WaterNoise(worldBuilder.Seed, CaveConfig.caveWater);

        foreach (var waterStart in localMinimas)
        {
            index++;

            var startPosition = waterStart;

            if (worldBuilder.IsCanceled || !waterNoise.IsWater(startPosition.x, startPosition.z) || IsWater(startPosition))
                continue;

            count++;

            HashSet<Vector3i> positions = ExpandWater(waterStart, cachedPrefabs);

            if (index % 100 == 0)
            {
                Logging.Debug($"Water processed: {count} / {index} / {localMinimas.Count}");
                yield return worldBuilder.SetMessage($"Water processing: {100.0f * index / localMinimas.Count:F0}%");
            }

            if (positions.Count > 0)
            {
                SetWater(positions);
            }
        }
    }

    public HashSet<Vector3i> SetWater(CavePrefabManager cachedPrefabs, HashSet<Vector3i> localMinimas)
    {
        // if (!CaveConfig.generateWater)
        //     yield break;

        int index = 0;
        var result = new HashSet<Vector3i>();

        foreach (var waterStart in localMinimas)
        {
            index++;

            if (IsWater(waterStart))
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

    public void SetRope(Vector3i position)
    {
        var hashZX = CaveBlock.HashZX(position.x + 1, position.z);
        var layers = caveblocks[hashZX];
        var layer = new RLELayer(0);

        for (int i = 0; i < layers.Count; i++)
        {
            layer.rawData = layers[i];
            layer.SetRope(true);
            layers[i] = layer.rawData;
        }
    }

    private void SetWater(HashSet<Vector3i> positions)
    {
        var layer = new RLELayer(0);

        foreach (var group in positions.GroupBy(p => CaveBlock.HashZX(p.x, p.z)))
        {
            var hashcode = group.Key;
            var waterLayer = new RLELayer(group, 1);
            var layers = caveblocks[hashcode].ToList();

            for (int i = layers.Count - 1; i >= 0; i--)
            {
                layer.rawData = layers[i];

                waterLayer.BlockRawData = layer.BlockRawData;
                waterLayer.SetWater(true);

                if (waterLayer.Start <= layer.Start && waterLayer.End >= layer.End)
                {
                    caveblocks[hashcode][i] = RLELayer.GetHashCode(
                        layer.Start,
                        layer.End,
                        waterLayer.BlockRawData
                    );
                }
                else if (waterLayer.Start <= layer.Start && waterLayer.End >= layer.Start && waterLayer.End < layer.End)
                {
                    caveblocks[hashcode].RemoveAt(i);

                    caveblocks[hashcode].Add(RLELayer.GetHashCode(
                        layer.Start,
                        waterLayer.End,
                        waterLayer.BlockRawData
                    ));

                    caveblocks[hashcode].Add(RLELayer.GetHashCode(
                        waterLayer.End + 1,
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
                var layer = new RLELayer(hashcode);

                CaveUtils.Assert(layer.Start <= layer.End, $"Invalid RLE Layer: start={layer.Start}, end={layer.End}");

                foreach (var block in layer.GetBlocks(x, z))
                {
                    yield return block;
                }
            }
        }
    }
}