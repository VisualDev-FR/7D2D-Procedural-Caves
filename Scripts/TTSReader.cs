using System;
using System.Collections.Generic;
using System.IO;

public class TTSReader
{
    public static List<Vector3i> LoadTerrainBlocks(string fullPath, int yOffset)
    {
        try
        {
            using (var baseStream = new FileStream(fullPath, FileMode.Open))
            {
                // using (PooledBinaryReader pooledBinaryReader = MemoryPools.poolBinaryReader.AllocSync(_bReset: false))
                using (var pooledBinaryReader = new BinaryReader(baseStream))
                {
                    // pooledBinaryReader.SetBaseStream(baseStream);
                    if (pooledBinaryReader.ReadChar() != 't' || pooledBinaryReader.ReadChar() != 't' || pooledBinaryReader.ReadChar() != 's' || pooledBinaryReader.ReadChar() != 0)
                    {
                        return null;
                    }

                    uint version = pooledBinaryReader.ReadUInt32();

                    return GetTerrainBlocks(pooledBinaryReader, version, yOffset);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"[Cave] Error while reading '{fullPath}': {e}");
            return null;
        }
    }

    private static Vector3i OffsetToCoord(int _offset, int size_x, int size_y)
    {
        int num = size_x * size_y;
        int z = _offset / num;
        _offset %= num;
        int y = _offset / size_x;
        int x = _offset % size_x;

        return new Vector3i(x, y, z);
    }

    private static bool BlockIsTerrain(BlockValue block)
    {
        return block.type < 256 && !block.isWater;
    }

    private static List<Vector3i> GetTerrainBlocks(BinaryReader _br, uint _version, int yOffset)
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

        int bufferSize = blockCount * 4;
        byte[] tempBuf = new byte[Utils.FastMax(200000, bufferSize)]; ;

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

                        if (y < -yOffset && (blockValue.isair || BlockIsTerrain(blockValue)))
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

                if (position.y < -yOffset && (blockValue.isair || BlockIsTerrain(blockValue)))
                {
                    result.Add(position);
                }
            }
            _br.Read(_data.m_Density, 0, size_x * size_y * size_z);
        }

        return result;
    }
}