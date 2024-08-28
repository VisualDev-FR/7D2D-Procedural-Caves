using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


// Light .tts file reader, to read prefab blocks datas from the world builder.
// His purpose is to collect and clusterize non tunnelable blocks from rwg-street-tile prefabs
public class TTSReader
{
    public static List<BoundingBox> Clusterize(string fullPath, int yOffset)
    {
        var blocks = ReadUndergroundBlocks(fullPath, yOffset);
        var clusters = ClusterizeBlocks(blocks);
        var result = new List<BoundingBox>();

        Log.Out($"{blocks.Count} blocks found.");

        foreach (var cluster in clusters)
        {
            var subVolumes = DivideCluster(cluster, blocks, 2);

            if (subVolumes.Count > 0)
            {
                result.AddRange(subVolumes);
            }
            else
            {
                result.Add(cluster);
            }
        }

        result = MergeBoundingBoxes(result);

        return result;
    }

    public static List<BoundingBox> DivideCluster(BoundingBox cluster, HashSet<Vector3i> blocks, int maxDeep)
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
                    break;
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

    public static BoundingBox TryMerge(BoundingBox a, BoundingBox b)
    {
        if (a.start.y == b.start.y && a.start.z == b.start.z && a.size.y == b.size.y && a.size.z == b.size.z)
        {
            if (a.start.x + a.size.x == b.start.x)
            {
                return new BoundingBox(a.start, new Vector3i(a.size.x + b.size.x, a.size.y, a.size.z));
            }

            if (b.start.x + b.size.x == a.start.x)
            {
                return new BoundingBox(b.start, new Vector3i(b.size.x + a.size.x, b.size.y, b.size.z));
            }
        }

        if (a.start.x == b.start.x && a.start.z == b.start.z && a.size.x == b.size.x && a.size.z == b.size.z)
        {
            if (a.start.y + a.size.y == b.start.y)
            {
                return new BoundingBox(a.start, new Vector3i(a.size.x, a.size.y + b.size.y, a.size.z));
            }

            if (b.start.y + b.size.y == a.start.y)
            {
                return new BoundingBox(b.start, new Vector3i(b.size.x, b.size.y + a.size.y, b.size.z));
            }
        }

        if (a.start.x == b.start.x && a.start.y == b.start.y && a.size.x == b.size.x && a.size.y == b.size.y)
        {
            if (a.start.z + a.size.z == b.start.z)
            {
                return new BoundingBox(a.start, new Vector3i(a.size.x, a.size.y, a.size.z + b.size.z));
            }

            if (b.start.z + b.size.z == a.start.z)
            {
                return new BoundingBox(b.start, new Vector3i(b.size.x, b.size.y, b.size.z + a.size.z));
            }
        }

        return null;
    }

    public static List<BoundingBox> MergeBoundingBoxes(List<BoundingBox> boxes)
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

    public static List<BoundingBox> Clusterize(PrefabInstance prefab)
    {
        var path = prefab.location.FullPath;
        var yOffset = prefab.prefab.yOffset;

        return Clusterize(path, yOffset);
    }

    // NOTE: copied from Prefab.loadBlockData
    public static HashSet<Vector3i> ReadUndergroundBlocks(string fullPath, int yOffset)
    {
        try
        {
            using (var stream = new FileStream(fullPath, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream))
                {
                    if (reader.ReadChar() != 't' || reader.ReadChar() != 't' || reader.ReadChar() != 's' || reader.ReadChar() != 0)
                    {
                        return null;
                    }

                    uint version = reader.ReadUInt32();

                    return GetUndergroundObstacles(reader, version, yOffset).ToHashSet();
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"[Cave] Error while reading .tts file '{fullPath}': {e}");
            return null;
        }
    }

    // NOTE: copied from Prefab.readBlockData
    private static List<Vector3i> GetUndergroundObstacles(BinaryReader _br, uint _version, int yOffset)
    {
        int size_x = _br.ReadInt16();
        int size_y = _br.ReadInt16();
        int size_z = _br.ReadInt16();
        int blockCount = size_x * size_y * size_z;

        var result = new List<Vector3i>();
        var blockValue = new BlockValue();
        var _data = new Prefab.Data();

        _data.Expand(blockCount);

        if (_version >= 2 && _version < 7)
        {
            _br.ReadBoolean(); // bCopyAirBlocks
        }
        if (_version >= 3 && _version < 7)
        {
            _br.ReadBoolean(); // bAllowTopSoilDecorations
        }

        byte[] tempBuf = new byte[Utils.FastMax(200_000, blockCount * 4)]; ;

        int cursor = 0;
        _br.Read(tempBuf, 0, blockCount * 4);

        if (_version <= 4)
        {
            for (int x = 0; x < size_x; x++)
            {
                for (int z = 0; z < size_z; z++)
                {
                    for (int y = 0; y < size_y; y++)
                    {
                        blockValue.rawData = (uint)(tempBuf[cursor] | (tempBuf[cursor + 1] << 8) | (tempBuf[cursor + 2] << 16) | (tempBuf[cursor + 3] << 24));
                        cursor += 4;

                        var position = new Vector3i(x, y, z);

                        if (IsObstacle(blockValue, position, yOffset))
                        {
                            result.Add(position);
                        }
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < blockCount; i++)
            {
                uint _rawData = (uint)(tempBuf[cursor] | (tempBuf[cursor + 1] << 8) | (tempBuf[cursor + 2] << 16) | (tempBuf[cursor + 3] << 24));
                cursor += 4;

                if (_rawData == 0)
                {
                    continue;
                }
                if (_version < 18)
                {
                    _rawData = BlockValueV3.ConvertOldRawData(_rawData);
                }
                blockValue.rawData = _rawData;

                var position = OffsetToCoord(i, size_x, size_y);

                if (IsObstacle(blockValue, position, yOffset))
                {
                    result.Add(position);
                }
            }
            _br.Read(_data.m_Density, 0, size_x * size_y * size_z);
        }

        return result;
    }

    // NOTE: copied from Prefab.offsetToCoord
    private static Vector3i OffsetToCoord(int _offset, int size_x, int size_y)
    {
        int num = size_x * size_y;
        int z = _offset / num;
        _offset %= num;
        int y = _offset / size_x;
        int x = _offset % size_x;

        return new Vector3i(x, y, z);
    }

    private static bool IsObstacle(BlockValue block, Vector3 position, int yOffset)
    {
        return position.y < -yOffset - CaveBuilder.terrainMargin && (block.type > 255 || block.isWater || block.isair);
    }

    private static bool IsInClusters(Vector3i pos, List<BoundingBox> clusters)
    {
        foreach (var rect in clusters)
        {
            if (CaveUtils.Intersect3D(pos.x, pos.y, pos.z, rect.start, rect.size))
            {
                return true;
            }
        }

        return false;
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

                clusterMin.x = CaveUtils.FastMin(clusterMin.x, currentPosition.x);
                clusterMin.y = CaveUtils.FastMin(clusterMin.y, currentPosition.y);
                clusterMin.z = CaveUtils.FastMin(clusterMin.z, currentPosition.z);

                clusterMax.x = CaveUtils.FastMax(clusterMax.x, currentPosition.x + 1);
                clusterMax.y = CaveUtils.FastMax(clusterMax.y, currentPosition.y + 1);
                clusterMax.z = CaveUtils.FastMax(clusterMax.z, currentPosition.z + 1);

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

            blockClusters.Add(new BoundingBox(null, clusterMin, clusterMax - clusterMin));
        }

        return blockClusters;
    }

}