using System;
using System.IO;

public class PrefabReader
{
    public static void Read(string fullPath)
    {
        Load(fullPath);
    }

    public static bool Load(string fullPath, bool _applyMapping = true, bool _fixChildblocks = true, bool _allowMissingBlocks = false, bool _skipLoadingBlockData = false)
    {
        if (!LoadBlockData(fullPath, _applyMapping, _fixChildblocks, _allowMissingBlocks, _skipLoadingBlockData))
        {
            return false;
        }

        return true;
    }

    public static bool LoadBlockData(string fullPath, bool _applyMapping, bool _fixChildblocks, bool _allowMissingBlocks, bool _skipLoadingBlockData = false)
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
                        return false;
                    }
                    uint version = pooledBinaryReader.ReadUInt32();

                    if (!ReadBlockData(pooledBinaryReader, version))
                    {
                        return false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"[Cave] Error while reading '{fullPath}': {e}");
            return false;
        }

        return true;
    }

    public static Vector3i OffsetToCoord(int _offset, int size_x, int size_y)
    {
        int num = size_x * size_y;
        int z = _offset / num;
        _offset %= num;
        int y = _offset / size_x;
        int x = _offset % size_x;

        return new Vector3i(x, y, z);
    }

    public static bool ReadBlockData(BinaryReader _br, uint _version)
    {
        int size_x = _br.ReadInt16();
        int size_y = _br.ReadInt16();
        int size_z = _br.ReadInt16();
        int blockCount = size_x * size_y * size_z;
        int totalBlocks = 0;

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
                        totalBlocks++;
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
                totalBlocks++;

                var position = OffsetToCoord(i, size_x, size_y);

                Log.Out($"{blockValue.type,-6}: {position}");
            }
            _br.Read(_data.m_Density, 0, size_x * size_y * size_z);
        }

        Log.Out($"total blocks: {totalBlocks}");

        return true;

    }

}