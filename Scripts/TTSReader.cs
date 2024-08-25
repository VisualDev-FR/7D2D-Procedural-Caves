using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Rect3D
{
    public Vector3i start;

    public Vector3i end;

    public Vector3i Size => end - start;

    public Rect3D(Vector3i start, Vector3i end)
    {
        this.start = start;
        this.end = end;
    }

    public override int GetHashCode()
    {
        return start.GetHashCode() + end.GetHashCode();
    }

    public static Rect3D RotateRectangle(int _rotCount, Rect3D marker, Vector3i tileSize, bool _bLeft = true)
    {
        var rectangle = new Rect3D(marker.start, marker.end);

        for (int i = 0; i < _rotCount; i++)
        {
            Vector3i size = rectangle.Size;
            Vector3i start = rectangle.start;
            Vector3i end = start + size;

            if (_bLeft)
            {
                start = new Vector3i(tileSize.z - start.z, start.y, start.x);
                end = new Vector3i(tileSize.z - end.z, end.y, end.x);
            }
            else
            {
                start = new Vector3i(start.z, start.y, tileSize.x - start.x);
                end = new Vector3i(end.z, end.y, tileSize.x - end.x);
            }
            if (start.x > end.x)
            {
                MathUtils.Swap(ref start.x, ref end.x);
            }
            if (start.z > end.z)
            {
                MathUtils.Swap(ref start.z, ref end.z);
            }

            rectangle.start = start;
            MathUtils.Swap(ref size.x, ref size.z);
            rectangle.end = start + size;

            MathUtils.Swap(ref tileSize.x, ref tileSize.z);
        }

        return rectangle;
    }

    public Rect3D Transform(Vector3i position, byte rotation, Vector3i tileSize)
    {
        var rectangle = RotateRectangle(rotation, this, tileSize);

        rectangle.start.x += position.x;
        rectangle.start.y += position.y;
        rectangle.start.z += position.z;

        rectangle.end.x += position.x;
        rectangle.end.y += position.y;
        rectangle.end.z += position.z;

        return rectangle;
    }
}

// Light .tts file reader, to read prefab blocks datas from the world builder.
// His purpose is to collect and clusterize non tunnelable blocks from rwg-street-tile prefabs
public class TTSReader
{
    // NOTE: copied from Prefab.loadBlockData
    public static List<Vector3i> GetUndergroundObstacles(string fullPath, int yOffset)
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

                    return GetUndergroundObstacles(reader, version, yOffset);
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
        int x = _offset / num;
        _offset %= num;
        int y = _offset / size_x;
        int z = _offset % size_x;

        return new Vector3i(x, y, z);
    }

    public static bool IsObstacle(BlockValue block, Vector3 position, int yOffset)
    {
        return position.y < -yOffset - CaveBuilder.terrainMargin && (block.type > 255 || block.isWater || block.isair);
    }

    public static bool IsInClusters(Vector3i pos, List<Rect3D> clusters)
    {
        foreach (var rect in clusters)
        {
            if (CaveUtils.Intersect3D(pos.x, pos.y, pos.z, rect.start, rect.Size))
            {
                return true;
            }
        }

        return false;
    }

    public static List<Rect3D> ClusterizeBlocks(HashSet<Vector3i> points)
    {
        var blockClusters = new List<Rect3D>();

        foreach (var start in points)
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

                    if (!cluster.Contains(position) && points.Contains(position))
                    {
                        queue.Add(position);
                    }
                }
            }

            blockClusters.Add(new Rect3D(clusterMin, clusterMax));
        }

        return blockClusters;
    }

}