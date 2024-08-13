using System.Collections;
using System.Collections.Generic;

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

    public void SetWater(int hashcode, bool value)
    {
        caveblocks[hashcode].isWater = true;
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

    public IEnumerator<CaveBlock> GetEnumerator()
    {
        return caveblocks.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}