using System.Collections.Generic;
using UnityEngine;

public class CaveSpawnManager
{
    public static System.Random rand = new System.Random();

    public static Vector3i GetSpawnPositionNearPlayer(Vector3 playerPosition, float minSpawnDist)
    {
        var timer = CaveUtils.StartTimer();
        var world = GameManager.Instance.World;
        var queue = new HashedPriorityQueue<AstarNode>();
        var visited = new HashSet<int>();
        var startNode = new AstarNode(new Vector3i(playerPosition));
        var sqrMinSpawnDist = minSpawnDist * minSpawnDist;
        var rolls = 0;

        queue.Enqueue(startNode, float.MaxValue);

        while (queue.Count > 0 && rolls++ < 200 && timer.ElapsedMilliseconds < 2)
        {
            AstarNode currentNode = queue.Dequeue();

            if (currentNode.SqrEuclidianDist(startNode) > sqrMinSpawnDist && world.CanMobsSpawnAtPos(currentNode.position))
            {
                // Logging.Info($"spawn position found at '{currentNode.position}', rolls: {rolls}, timer: {timer.ElapsedMilliseconds}ms");
                return currentNode.position;
            }

            int x = currentNode.position.x;
            int y = currentNode.position.y;
            int z = currentNode.position.z;

            visited.Add(currentNode.hashcode);

            foreach (var offset in CaveUtils.offsetsNoVertical)
            {
                Vector3i neighborPos = currentNode.position + offset;
                uint rawData = world.GetBlock(x + offset.x, y + offset.y, z + offset.z).rawData;

                // TODO: revise this criteria which is too much restrictive
                bool canExtend =
                       !visited.Contains(neighborPos.GetHashCode())
                    && (rawData == 0 || rawData > 255)
                    && world.GetBlock(x + offset.x, y + offset.y - 1, z + offset.z).rawData < 256;

                if (!canExtend)
                    continue;

                var neighbor = new AstarNode(neighborPos, currentNode);

                queue.Enqueue(neighbor, -neighbor.totalDist + rand.Next(-1, 2));
            }
        }

        // reaching here means that no spawn block was found
        return Vector3i.zero;
    }

}