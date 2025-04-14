using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using WorldGenerationEngineFinal;


public class CaveMap
{
    private readonly Dictionary<int, List<int>> rleLayers;

    public int BlocksCount => rleLayers.Values.Sum(layers => layers.Sum(layerHash => RLELayer.Count(layerHash)));

    public readonly int worldSize;

    public int TunnelsCount { get; private set; }

    private readonly object _lock = new object();

    public CaveMap(int worldSize)
    {
        this.worldSize = worldSize;
        rleLayers = new Dictionary<int, List<int>>();
    }

    public void Cleanup()
    {
        foreach (var list in rleLayers.Values)
        {
            list.Clear();
        }

        rleLayers.Clear();
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

            if (!rleLayers.ContainsKey(hashZX))
            {
                lock (_lock)
                {
                    rleLayers[hashZX] = new List<int>();
                }
            }

            foreach (var layerHash in RLEEncode(group))
            {
                lock (_lock)
                {
                    rleLayers[hashZX].Add(layerHash);
                }
            }
        }
    }

    public void AddTunnel(CaveTunnel tunnel)
    {
        AddBlocks(tunnel.blocks);
        TunnelsCount++;
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

        if (!rleLayers.ContainsKey(hashZX))
            return false;

        var layer = new RLELayer(0);

        foreach (var layerHash in rleLayers[hashZX])
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

        if (!rleLayers.ContainsKey(hashZX))
            return false;

        var layer = new RLELayer(0);

        foreach (var layerHash in rleLayers[hashZX])
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
            foreach (var entry in rleLayers)
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

    private HashSet<Vector3i> ExpandWater(CaveBlock waterStart, CavePrefabManager cachedPrefabs)
    {
        CaveUtils.Assert(waterStart is CaveBlock, "null water start");

        var queue = new Queue<Vector3i>(1_000);
        var visited = new HashSet<Vector3i>(100_000);
        var waterPositions = new HashSet<Vector3i>(100_000);
        var startPosition = GetVerticalLowerPoint(waterStart.ToVector3i());
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

            foreach (var offset in BFSUtils.offsets)
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

            var startPosition = waterStart.ToVector3i();

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

    public void SetRope(Vector3i position)
    {
        var hashZX = CaveBlock.HashZX(position.x + 1, position.z);
        var layers = rleLayers[hashZX];
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
            var layers = rleLayers[hashcode].ToList();

            for (int i = layers.Count - 1; i >= 0; i--)
            {
                layer.rawData = layers[i];

                waterLayer.BlockRawData = layer.BlockRawData;
                waterLayer.SetWater(true);

                if (waterLayer.Start <= layer.Start && waterLayer.End >= layer.End)
                {
                    rleLayers[hashcode][i] = RLELayer.GetHashCode(
                        layer.Start,
                        layer.End,
                        waterLayer.BlockRawData
                    );
                }
                else if (waterLayer.Start <= layer.Start && waterLayer.End >= layer.Start && waterLayer.End < layer.End)
                {
                    rleLayers[hashcode].RemoveAt(i);

                    rleLayers[hashcode].Add(RLELayer.GetHashCode(
                        layer.Start,
                        waterLayer.End,
                        waterLayer.BlockRawData
                    ));

                    rleLayers[hashcode].Add(RLELayer.GetHashCode(
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
        foreach (var entry in rleLayers)
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