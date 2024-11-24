using System.Collections.Generic;
using System.Linq;

public class BlockClusterizer
{
    public static List<BoundingBox> Clusterize(PrefabInstance prefabInstance)
    {
        var blocks = TTSReader.ReadUndergroundBlocks(prefabInstance);
        return Clusterize(blocks);
    }

    public static List<BoundingBox> Clusterize(PrefabDataInstance pdi)
    {
        var blocks = TTSReader.ReadUndergroundBlocks(pdi);
        return Clusterize(blocks);
    }

    private static List<BoundingBox> Clusterize(HashSet<Vector3i> blocks)
    {
        var clusters = ClusterizeBlocks(blocks);
        // var merged = new List<BoundingBox>();

        // Logging.Info($"{blocks.Count} blocks found.");

        // foreach (var cluster in clusters)
        // {
        //     var subVolumes = DivideCluster(cluster, blocks, 2);

        //     if (subVolumes.Count > 0)
        //     {
        //         merged.AddRange(subVolumes);
        //     }
        //     else
        //     {
        //         merged.Add(cluster);
        //     }
        // }

        // merged = MergeBoundingBoxes(merged);

        return clusters;
    }

    private static List<BoundingBox> ClusterizeBlocks(HashSet<Vector3i> blockPositions)
    {
        var blockClusters = new List<BoundingBox>();

        foreach (var start in blockPositions)
        {
            if (IsInClusters(start, blockClusters))
                continue;

            var clusterMin = new Vector3i(int.MaxValue, int.MaxValue, int.MaxValue);
            var clusterMax = new Vector3i(int.MinValue, int.MinValue, int.MinValue);

            var queue = new HashSet<Vector3i>() { start };
            var cluster = new HashSet<Vector3i>();
            var index = 100_000;

            while (queue.Count > 0 && index-- > 0)
            {
                Vector3i currentPosition = queue.First();

                clusterMin.x = Utils.FastMin(clusterMin.x, currentPosition.x);
                clusterMin.y = Utils.FastMin(clusterMin.y, currentPosition.y);
                clusterMin.z = Utils.FastMin(clusterMin.z, currentPosition.z);

                clusterMax.x = Utils.FastMax(clusterMax.x, currentPosition.x + 1);
                clusterMax.y = Utils.FastMax(clusterMax.y, currentPosition.y + 1);
                clusterMax.z = Utils.FastMax(clusterMax.z, currentPosition.z + 1);

                cluster.Add(currentPosition);
                queue.Remove(currentPosition);

                foreach (var offset in CaveUtils.offsets)
                {
                    var position = currentPosition + offset;

                    if (!cluster.Contains(position) && blockPositions.Contains(position))
                    {
                        queue.Add(position);
                    }
                }
            }

            blockClusters.Add(new BoundingBox(clusterMin, clusterMax - clusterMin, cluster.Count));
        }

        return blockClusters;
    }

    private static List<BoundingBox> DivideCluster(BoundingBox cluster, HashSet<Vector3i> blocks, int maxDeep)
    {
        var result = new List<BoundingBox>();

        foreach (var bb in cluster.Octree())
        {
            bool containsBlock = false;

            foreach (var pos in bb.IteratePoints())
            {
                if (blocks.Contains(pos))
                {
                    containsBlock = true;
                    bb.blocksCount++;
                }
            }

            if (containsBlock && maxDeep > 0)
            {
                result.AddRange(DivideCluster(bb, blocks, maxDeep - 1));
            }
            else if (containsBlock)
            {
                result.Add(bb);
            }

        }

        return result;
    }

    private static BoundingBox TryMerge(BoundingBox a, BoundingBox b)
    {
        if (a.start.y == b.start.y && a.start.z == b.start.z && a.size.y == b.size.y && a.size.z == b.size.z)
        {
            if (a.start.x + a.size.x == b.start.x)
            {
                return new BoundingBox(a.start, new Vector3i(a.size.x + b.size.x, a.size.y, a.size.z), a.blocksCount + b.blocksCount);
            }

            if (b.start.x + b.size.x == a.start.x)
            {
                return new BoundingBox(b.start, new Vector3i(b.size.x + a.size.x, b.size.y, b.size.z), a.blocksCount + b.blocksCount);
            }
        }

        if (a.start.x == b.start.x && a.start.z == b.start.z && a.size.x == b.size.x && a.size.z == b.size.z)
        {
            if (a.start.y + a.size.y == b.start.y)
            {
                return new BoundingBox(a.start, new Vector3i(a.size.x, a.size.y + b.size.y, a.size.z), a.blocksCount + b.blocksCount);
            }

            if (b.start.y + b.size.y == a.start.y)
            {
                return new BoundingBox(b.start, new Vector3i(b.size.x, b.size.y + a.size.y, b.size.z), a.blocksCount + b.blocksCount);
            }
        }

        if (a.start.x == b.start.x && a.start.y == b.start.y && a.size.x == b.size.x && a.size.y == b.size.y)
        {
            if (a.start.z + a.size.z == b.start.z)
            {
                return new BoundingBox(a.start, new Vector3i(a.size.x, a.size.y, a.size.z + b.size.z), a.blocksCount + b.blocksCount);
            }

            if (b.start.z + b.size.z == a.start.z)
            {
                return new BoundingBox(b.start, new Vector3i(b.size.x, b.size.y, b.size.z + a.size.z), a.blocksCount + b.blocksCount);
            }
        }

        return null;
    }

    private static List<BoundingBox> MergeBoundingBoxes(List<BoundingBox> boxes)
    {
        bool merged;

        do
        {
            merged = false;
            for (int i = 0; i < boxes.Count; i++)
            {
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    var newBox = TryMerge(boxes[i], boxes[j]);
                    if (newBox != null)
                    {
                        boxes[i] = newBox;
                        boxes.RemoveAt(j);
                        merged = true;
                        break; // Sort de la boucle intérieure après une fusion
                    }
                }
                if (merged)
                {
                    break; // Sort de la boucle extérieure pour recommencer à zéro
                }
            }
        } while (merged);

        return boxes;
    }

    private static bool IsInClusters(Vector3i pos, List<BoundingBox> clusters)
    {
        foreach (var rect in clusters)
        {
            if (CaveUtils.Intersect3D(pos, rect.start, rect.size))
            {
                return true;
            }
        }

        return false;
    }

}