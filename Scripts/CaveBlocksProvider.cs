using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


public class CaveChunksProvider
{
    public string cavemapDir;

    public CaveGraph caveGraph;

    public Dictionary<int, CaveRegion> regions;

    public CaveChunksProvider(string worldName)
    {
        regions = new Dictionary<int, CaveRegion>();
        cavemapDir = $"{GameIO.GetWorldDir(worldName)}/cavemap";
        caveGraph = new CaveGraph($"{GameIO.GetWorldDir(worldName)}/cavegraph.txt");
    }

    public static int GetRegionID(Vector2s chunkPos)
    {
        var region_x = chunkPos.x / CaveBuilder.chunkRegionGridSize;
        var region_z = chunkPos.z / CaveBuilder.chunkRegionGridSize;

        var regionID = region_x + region_z * CaveBuilder.regionGridSize;

        return regionID;
    }

    public static int HashCodeFromWorldPos(Vector3 worldPos)
    {
        return HashCodeFromWorldPos(
            (int)worldPos.x,
            (int)worldPos.y,
            (int)worldPos.z
        );
    }

    public static int HashCodeFromWorldPos(int x, int y, int z)
    {
        int halfWorldSize = CaveBuilder.worldSize / 2;

        return CaveBlock.GetHashCode(
            x + halfWorldSize,
            y,
            z + halfWorldSize
        );
    }

    public static Vector2s GetChunkPos(Chunk chunk)
    {
        return new Vector2s(
            (short)(chunk.ChunkPos.x + (CaveBuilder.worldSize >> 5)),
            (short)(chunk.ChunkPos.z + (CaveBuilder.worldSize >> 5))
        );
    }

    public static Vector2s GetChunkPos(Vector3 worldPos)
    {
        return GetChunkPos(
            (short)worldPos.x,
            (short)worldPos.z
        );
    }

    public static Vector2s GetChunkPos(short worldX, short worldZ)
    {
        return new Vector2s(
            (short)((worldX >> 4) + (CaveBuilder.worldSize >> 5)),
            (short)((worldZ >> 4) + (CaveBuilder.worldSize >> 5))
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

        return CaveUtils.SqrEuclidianDist(block.ToWorldPos(), playerpos) > minSpawnDist * minSpawnDist;
    }

    public List<CaveBlock> GetSpawnPositionsFromPlayer(Vector3 playerPosition, int minSpawnDist)
    {
        var caveBlocks = new HashSet<CaveBlock>();
        var visitedChunks = new HashSet<Vector2s>();
        var worldSize = CaveBuilder.worldSize;
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
