using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


public class CaveChunksProvider
{
    public string cavemapDir;

    public CaveGraph caveGraph;

    public Dictionary<int, CaveRegion> regions;

    public int worldSize;

    public CaveChunksProvider(string worldName, int worldSize)
    {
        this.worldSize = worldSize;

        regions = new Dictionary<int, CaveRegion>();
        cavemapDir = $"{GameIO.GetWorldDir(worldName)}/cavemap";
    }

    public int GetRegionID(Vector2s chunkPos)
    {
        int chunkRegionGridSize = CaveConfig.RegionSize >> 4;
        int regionGridSize = worldSize / CaveConfig.RegionSize;

        int region_x = chunkPos.x / chunkRegionGridSize;
        int region_z = chunkPos.z / chunkRegionGridSize;

        int regionID = region_x + region_z * regionGridSize;

        return regionID;
    }

    public int HashCodeFromWorldPos(Vector3 worldPos)
    {
        return HashCodeFromWorldPos(
            (int)worldPos.x,
            (int)worldPos.y,
            (int)worldPos.z
        );
    }

    public int HashCodeFromWorldPos(int x, int y, int z)
    {
        int halfWorldSize = worldSize / 2;

        return CaveBlock.GetHashCode(
            x + halfWorldSize,
            y,
            z + halfWorldSize
        );
    }

    public Vector2s GetChunkPos(Chunk chunk)
    {
        return new Vector2s(
            (short)(chunk.ChunkPos.x + (worldSize >> 5)),
            (short)(chunk.ChunkPos.z + (worldSize >> 5))
        );
    }

    public Vector2s GetChunkPos(Vector3 worldPos)
    {
        return GetChunkPos(
            (short)worldPos.x,
            (short)worldPos.z
        );
    }

    public Vector2s GetChunkPos(short worldX, short worldZ)
    {
        return new Vector2s(
            (short)((worldX >> 4) + (worldSize >> 5)),
            (short)((worldZ >> 4) + (worldSize >> 5))
        );
    }

    private CaveRegion CreateCaveRegion(int regionID)
    {
        string filename = $"{cavemapDir}/region_{regionID}.bin";

        if (!File.Exists(filename))
        {
            Log.Warning($"[Cave] cave region not found 'region_{regionID}'");
            return null;
        }

        regions[regionID] = new CaveRegion(filename);

        return regions[regionID];
    }

    public CaveRegion GetRegion(Vector2s chunkPos)
    {
        int regionID = GetRegionID(chunkPos);

        if (regions.TryGetValue(regionID, out var region))
        {
            return region;
        }

        return CreateCaveRegion(regionID);
    }

    public CaveChunk GetCaveChunk(Vector3 worldPos)
    {
        return GetCaveChunk(
            (short)worldPos.x,
            (short)worldPos.z
        );
    }

    public CaveChunk GetCaveChunk(Chunk chunk)
    {
        var chunkPos = GetChunkPos(chunk);
        var caveRegion = GetRegion(chunkPos);

        return caveRegion?.GetCaveChunk(chunkPos);
    }

    public CaveChunk GetCaveChunk(short worldX, short worldZ)
    {
        var chunkPos = GetChunkPos(worldX, worldZ);
        var caveRegion = GetRegion(chunkPos);

        return caveRegion?.GetCaveChunk(chunkPos);
    }

    public HashSet<CaveBlock> GetCaveBlocks(Vector2s chunkPos)
    {
        var caveRegion = GetRegion(chunkPos);

        if (caveRegion == null)
        {
            return null;
        }

        return caveRegion.GetCaveBlocks(chunkPos);
    }

    public HashSet<CaveBlock> GetCaveBlocks(Chunk chunk)
    {
        var chunkPos = GetChunkPos(chunk);
        return GetCaveBlocks(chunkPos);
    }

    public CaveBlock GetCaveBlock(Vector3 worldPos)
    {
        var caveChunk = GetCaveChunk(worldPos);
        var hashcode = HashCodeFromWorldPos(worldPos);

        if (caveChunk is null)
            return null;

        return caveChunk.GetBlock(hashcode);
    }

    public bool IsCave(int worldX, int worldY, int worldZ)
    {
        var caveChunk = GetCaveChunk((short)worldX, (short)worldZ);
        var hashcode = HashCodeFromWorldPos(worldX, worldY, worldZ);

        if (caveChunk == null)
            return false;

        return caveChunk.Exists(hashcode);
    }

    public bool IsCave(Vector3 worldPos)
    {
        var caveChunk = GetCaveChunk((short)worldPos.x, (short)worldPos.x);
        var hashcode = HashCodeFromWorldPos(worldPos);

        if (caveChunk == null)
            return false;

        return caveChunk.Exists(hashcode);
    }

    public HashSet<int> GetTunnelsAroundPrefab(PrefabInstance prefabInstance)
    {
        // TODO: re-setup cave graph saving at world generation before enabling this code
        return null;

        if (caveGraph.graph.TryGetValue(prefabInstance.id, out var tunnelIDs))
        {
            return tunnelIDs.ToHashSet();
        }

        return null;
    }

    public HashSet<int> FindTunnelsNearPosition(Vector3 playerPosition)
    {
        if (GetCaveBlock(playerPosition) is CaveBlock block)
        {
            return new HashSet<int>() { block.tunnelID.value };
        }

        if (GameManager.Instance.World.GetPOIAtPosition(playerPosition) is PrefabInstance prefabInstance)
        {
            return GetTunnelsAroundPrefab(prefabInstance);
        }

        var queue = new HashSet<Vector3>() { playerPosition };
        var visited = new HashSet<Vector3>() { };
        var maxRolls = 1000;

        while (queue.Count > 0 && maxRolls-- > 0)
        {
            var currentPos = queue.First();

            if (GetCaveBlock(currentPos) is CaveBlock caveBlock)
            {
                return new HashSet<int>() { caveBlock.tunnelID.value };
            }

            visited.Add(currentPos);
            queue.Remove(currentPos);

            foreach (var offset in CaveUtils.offsets)
            {
                var position = currentPos + offset;

                if (visited.Contains(position))
                    continue;

                var blockType = GameManager.Instance.World.GetBlock((int)position.x, (int)position.y, (int)position.z).type;

                if (blockType > 0 && blockType < 255 && position.y < GameManager.Instance.World.GetHeight((int)position.x, (int)position.z))
                {
                    continue;
                }

                queue.Add(position);
            }
        }

        return null;
    }

    public bool CanSpawnEnemyAt(CaveBlock block, Vector3 playerpos, int minSpawnDist, HashSet<int> tunnelIDs)
    {
        if (!block.isFloor || !block.isFlat || block.isWater || (tunnelIDs != null && !tunnelIDs.Contains(block.tunnelID.value)))
        {
            return false;
        }

        return CaveUtils.SqrEuclidianDist(block.ToWorldPos(CaveGenerator.HalfWorldSize), playerpos) > minSpawnDist * minSpawnDist;
    }

    public List<CaveBlock> GetSpawnPositionsFromPlayer(Vector3 playerPosition, int minSpawnDist)
    {
        // TODO: debug game crashes on 8k maps
        return new List<CaveBlock>();

        var caveBlocks = new HashSet<CaveBlock>();
        var visitedChunks = new HashSet<Vector2s>();
        var chunkPos = GetChunkPos(playerPosition);
        var tunnelIDs = FindTunnelsNearPosition(playerPosition);

        var queue = new Queue<Vector2s>();
        queue.Enqueue(chunkPos);
        visitedChunks.Add(chunkPos);

        while (queue.Count > 0 && caveBlocks.Count == 0)
        {
            var currentChunkPos = queue.Dequeue();

            var blocks = GetCaveBlocks(currentChunkPos);

            if (blocks != null)
            {
                var spawnableBlocks = blocks.Where(block => CanSpawnEnemyAt(block, playerPosition, minSpawnDist, tunnelIDs));
                caveBlocks.UnionWith(spawnableBlocks);
            }

            foreach (var offset in CaveUtils.offsets)
            {
                var neighborChunkPos = new Vector2s(
                    (short)(currentChunkPos.x + offset.x),
                    (short)(currentChunkPos.z + offset.z)
                );

                if (!visitedChunks.Contains(neighborChunkPos))
                {
                    queue.Enqueue(neighborChunkPos);
                    visitedChunks.Add(neighborChunkPos);
                }
            }
        }

        return caveBlocks.ToList();
    }
}
