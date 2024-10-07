using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using WorldGenerationEngineFinal;

public class CaveMap : IEnumerable<CaveBlock>
{
    private readonly Vector3i position;

    private readonly Vector3i size;

    private readonly CavePrefabManager cachedPrefabs;

    private readonly Dictionary<int, CaveBlock> caveblocks;

    public readonly Dictionary<int, CaveTunnel> tunnels;

    public int Count => caveblocks.Count;

    public int TunnelsCount => tunnels.Count;

    public CaveMap(Vector3i position, Vector3i size, CavePrefabManager cachedPrefabs)
    {
        caveblocks = new Dictionary<int, CaveBlock>();

        this.position = position;
        this.size = size;
        this.cachedPrefabs = cachedPrefabs;
    }

    public CaveMap()
    {
        caveblocks = new Dictionary<int, CaveBlock>();
        tunnels = new Dictionary<int, CaveTunnel>();
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

    public void AddRoom(CaveRoom room)
    {
        foreach (var pos in room.GetBlocks())
        {
            caveblocks[pos.GetHashCode()] = new CaveBlock(pos)
            {
                isRoom = true,
            };
        }
    }

    public void AddTunnel(CaveTunnel tunnel)
    {
        tunnel.SetID(tunnels.Count);
        tunnels[tunnels.Count] = tunnel;

        foreach (var block in tunnel.blocks)
        {
            caveblocks[block.GetHashCode()] = block;
        }
    }

    public void __AddTunnel(CaveTunnel tunnel)
    {
        var intersectedTunnels = new HashSet<int>() { tunnel.id.value };

        foreach (var block in tunnel.blocks)
        {
            int hashcode = block.GetHashCode();

            if (caveblocks.ContainsKey(hashcode))
            {
                intersectedTunnels.Add(caveblocks[hashcode].tunnelID.value);
            }
            else
            {
                caveblocks[hashcode] = block;
            }
        }

        Log.Out($"existing tunnels: {string.Join(", ", tunnels.Keys)}");
        CaveUtils.Assert(!tunnels.ContainsKey(tunnel.id.value), $"existing key: {tunnel.id.value}");

        tunnels.Add(tunnel.id.value, tunnel);

        Log.Out($"add tunnel {tunnel.id}");

        if (intersectedTunnels.Count > 1)
        {
            MergeTunnels(intersectedTunnels);
        }
    }

    private void MergeTunnels(HashSet<int> tunnelIDs)
    {
        Log.Out($"merge tunnels {string.Join(", ", tunnelIDs)}");

        var mainTunnelID = tunnelIDs.Min();
        var mainTunnel = tunnels[mainTunnelID];

        foreach (var tunnelID in tunnelIDs)
        {
            if (tunnelID == mainTunnelID || !tunnels.ContainsKey(tunnelID))
                continue;

            CaveUtils.Assert(tunnels.ContainsKey(tunnelID), $"MissingKey: {tunnelID}");

            mainTunnel.blocks.UnionWith(tunnels[tunnelID].blocks);
            tunnels[tunnelID].SetID(mainTunnelID);
            tunnels.Remove(tunnelID);

            Log.Out($"remove tunnel {tunnelID}");
        }

        Log.Out($"remaining tunnels: {string.Join(", ", tunnels.Keys)}");
    }

    public void Save(string dirname)
    {
        using (var multistream = new MultiStream(dirname, create: true))
        {
            foreach (CaveBlock caveBlock in caveblocks.Values)
            {
                int region_x = caveBlock.x / CaveConfig.RegionSize;
                int region_z = caveBlock.z / CaveConfig.RegionSize;
                int regionID = region_x + region_z * CaveConfig.RegionGridSize;

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

    private HashSet<int> ExpandWater(CaveBlock waterStart, CavePrefabManager cachedPrefabs)
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
            waterHashes.Add(currentHash);

            foreach (int offsetHash in CaveUtils.offsetHashes)
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
                    caveblocks.ContainsKey(neighborHash)
                    && !visited.Contains(neighborHash)
                    && caveblocks[neighborHash].y <= startPosition.y;

                if (shouldEnqueue)
                {
                    queue.Enqueue(neighborHash);
                }
            }
        }

        return waterHashes;
    }

    public void SetWater(HashSet<CaveBlock> localMinimas, CavePrefabManager cachedPrefabs)
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

    public IEnumerator SetWaterCoroutine(WorldBuilder worldBuilder, HashSet<CaveBlock> localMinimas)
    {
        int index = 0;

        if (!CaveConfig.generateWater)
            yield break;

        foreach (var waterStart in localMinimas)
        {
            index++;

            if (worldBuilder.IsCanceled)
                yield break;

            if (waterStart.isWater)
                continue;

            HashSet<int> hashcodes = ExpandWater(waterStart, cachedPrefabs);

            string message = $"Water processing: {100.0f * index / localMinimas.Count:F0}% ({index} / {localMinimas.Count})";

            yield return worldBuilder.SetMessage(message);

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