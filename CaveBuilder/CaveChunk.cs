using System.Collections.Generic;
using System.Linq;

public class CaveChunk
{
    private readonly Dictionary<int, CaveBlock> caveBlocks;

    public CaveChunk()
    {
        caveBlocks = new Dictionary<int, CaveBlock>();
    }

    public void AddBlock(CaveBlock block)
    {
        caveBlocks[block.GetHashCode()] = block;
    }

    public HashSet<CaveBlock> GetBlocks()
    {
        return caveBlocks.Values.ToHashSet();
    }

    public CaveBlock GetBlock(int hashcode)
    {
        if (caveBlocks.TryGetValue(hashcode, out var block))
        {
            return block;
        }

        return null;
    }

    public bool Exists(int hashcode)
    {
        return caveBlocks.ContainsKey(hashcode);
    }
}