using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


// Light .tts file reader, to read prefab blocks datas from the world builder.
// His purpose is to collect and clusterize non-terrain / underground blocks from prefabs
// to create volumes where the caveBuilder is not allowed to generate tunnels
public class TTSReader
{
    public static HashSet<Vector3i> ReadUndergroundBlocks(PrefabInstance prefabInstance)
    {
        var fullPath = prefabInstance.location.FullPath;
        var yOffset = prefabInstance.prefab.yOffset;

        return ReadUndergroundBlocks(fullPath, yOffset);
    }

    public static HashSet<Vector3i> ReadUndergroundBlocks(PrefabDataInstance pdi)
    {
        var fullPath = pdi.location.FullPath;
        var yOffset = pdi.prefab.yOffset;

        return ReadUndergroundBlocks(fullPath, yOffset);
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

                        if (!IsUnderTerrain(y, yOffset))
                            continue;

                        if (IsObstacle(blockValue))
                        {
                            result.Add(new Vector3i(x, y, z));
                        }
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < blockCount; i++)
            {
                var position = OffsetToCoord(i, size_x, size_y);
                var _rawData = (uint)(tempBuf[cursor] | (tempBuf[cursor + 1] << 8) | (tempBuf[cursor + 2] << 16) | (tempBuf[cursor + 3] << 24));

                cursor += 4;

                if (!IsUnderTerrain(position.y, yOffset))
                    continue;

                if (_version < 18)
                {
                    _rawData = BlockValueV3.ConvertOldRawData(_rawData);
                }

                blockValue.rawData = _rawData;

                if (IsObstacle(blockValue))
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

    private static bool IsUnderTerrain(int posY, int yOffset)
    {
        return posY < -yOffset - CaveConfig.terrainMargin;
    }

    private static bool IsObstacle(BlockValue block)
    {
        return block.rawData == 0 || block.type > 255 || block.isWater;
    }

}