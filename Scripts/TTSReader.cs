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
            var subVolumes = DivideCluster(cluster, blocks, 3);

            if (subVolumes.Count > 0)
            {
                result.AddRange(subVolumes);
            }
            else
            {
                result.Add(cluster);
            }
        }

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

            if (containsBlock)
            {
                if (maxDeep > 0)
                {
                    result.AddRange(DivideCluster(bb, blocks, maxDeep - 1));
                }
                else
                {
                    result.Add(bb);
                }
            }
        }

        return result;
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