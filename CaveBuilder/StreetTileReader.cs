using System;
using System.Collections.Generic;
using System.IO;

public static class StreetTileReader
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
        ArrayListMP<int> arrayListMP = null;

        // if (_applyMapping)
        // {
        //     arrayListMP = LoadIdMapping(_location.Folder, _location.FileNameNoExtension, _allowMissingBlocks);
        //     if (arrayListMP == null)
        //     {
        //         return false;
        //     }
        // }
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
                    uint num = pooledBinaryReader.ReadUInt32();
                    if (!ReadBlockData(pooledBinaryReader, num, arrayListMP?.Items, _fixChildblocks: true))
                    {
                        return false;
                    }
                    // if (num > 12)
                    // {
                    //     readTileEntities(pooledBinaryReader);
                    // }
                    // if (num > 15)
                    // {
                    //     readTriggerData(pooledBinaryReader);
                    // }
                    // insidePos.Load(_location.FullPathNoExtension + ".ins", size);
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

    public static bool ReadBlockData(BinaryReader _br, uint _version, int[] _blockIdMapping, bool _fixChildblocks)
    {
        // statistics.Clear();
        // multiBlockParentIndices.Clear();
        // decoAllowedBlockIndices.Clear();
        // localRotation = 0;

        int size_x = _br.ReadInt16();
        int size_y = _br.ReadInt16();
        int size_z = _br.ReadInt16();

        int blockCount = size_x * size_y * size_z;
        int totalBlocks = 0;

        // InitData();
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

        List<Vector3i> list = null;
        int blockID = -1; // blockTypeMissingBlock;
        int bufferSize = blockCount * 4;
        byte[] tempBuf = new byte[Utils.FastMax(200000, bufferSize)]; ;

        if (_blockIdMapping != null && blockID >= 0)
        {
            list = new List<Vector3i>();
        }

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
                        BlockValue _blockValue = new BlockValue((uint)(tempBuf[cursor] | (tempBuf[cursor + 1] << 8) | (tempBuf[cursor + 2] << 16) | (tempBuf[cursor + 3] << 24)));
                        cursor += 4;
                        totalBlocks++;
                        // if (_blockIdMapping != null)
                        // {
                        //     int blockType = _blockIdMapping[_blockValue.type];
                        //     if (blockType < 0)
                        //     {
                        //         Log.Error("Loading prefab \"" + _location.ToString() + "\" failed: Block " + _blockValue.type + " used in prefab has no mapping.");
                        //         return false;
                        //     }
                        //     _blockValue.type = blockType;
                        //     if (blockID >= 0 && _blockValue.type == blockTypeMissingBlock)
                        //     {
                        //         list.Add(new Vector3i(x, y, z));
                        //     }
                        // }
                        // if (_blockValue.isWater)
                        // {
                        //     SetWater(x, y, z, WaterValue.Full);
                        //     continue;
                        // }
                        // if (_fixChildblocks)
                        // {
                        //     if (_blockValue.ischild)
                        //     {
                        //         continue;
                        //     }
                        //     Block block = _blockValue.Block;
                        //     if (block == null || ((_blockValue.meta & (true ? 1u : 0u)) != 0 && block is BlockModelTree))
                        //     {
                        //         continue;
                        //     }
                        // }
                        // SetBlock(x, y, z, _blockValue);
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
                // _data.m_Blocks[i] = 0u;
                if (_rawData == 0)
                {
                    continue;
                }
                if (_version < 18)
                {
                    _rawData = BlockValueV3.ConvertOldRawData(_rawData);
                }
                BlockValue blockValue = new BlockValue(_rawData);
                totalBlocks++;

                // if (_blockIdMapping != null)
                // {
                //     int type = blockValue.type;
                //     if (type != 0)
                //     {
                //         int num6 = _blockIdMapping[type];
                //         if (num6 < 0)
                //         {
                //             offsetToCoord(i, out var _x, out var _y, out var _z);
                //             Log.Error("Loading prefab \"" + location.ToString() + "\" failed: Block " + type + " used in prefab at " + _x + " / " + _y + " / " + _z + " has no mapping.");
                //             return false;
                //         }
                //         blockValue.type = num6;
                //         if (blockID >= 0 && num6 == blockTypeMissingBlock)
                //         {
                //             offsetToCoord(i, out var _x2, out var _y2, out var _z2);
                //             list.Add(new Vector3i(_x2, _y2, _z2));
                //         }
                //     }
                // }
                // if (_version < 17 && blockValue.isWater)
                // {
                //     _data.m_Water[i] = WaterValue.Full;
                //     continue;
                // }
                // Block block2 = blockValue.Block;
                // updateBlockStatistics(blockValue, block2);
                // if (!_fixChildblocks || (!blockValue.ischild && block2 != null && ((blockValue.meta & 1) == 0 || !(block2 is BlockModelTree))))
                // {
                //     if (block2.isMultiBlock && !blockValue.ischild)
                //     {
                //         multiBlockParentIndices.Add(i);
                //     }
                //     if (DecoUtils.HasDecoAllowed(blockValue))
                //     {
                //         decoAllowedBlockIndices.Add(i);
                //     }
                //     _data.m_Blocks[i] = blockValue.rawData;
                // }
            }
            _br.Read(_data.m_Density, 0, size_x * size_y * size_z);
        }

        Log.Out($"total blocks: {totalBlocks}");

        return true;

        // if (_blockIdMapping != null && blockID >= 0)
        // {
        //     foreach (Vector3i item in list)
        //     {
        //         SetDensity(item.x, item.y, item.z, MarchingCubes.DensityAir);
        //     }
        // }
        // if (_version > 8)
        // {
        //     _br.Read(tempBuf, 0, blockCount * 2);
        //     for (int m = 0; m < blockCount; m++)
        //     {
        //         _data.m_Damage[m] = (ushort)(tempBuf[m * 2] | (tempBuf[m * 2 + 1] << 8));
        //     }
        // }
        // if (_version >= 10)
        // {
        //     simpleBitStreamReader.Reset();
        //     simpleBitStreamReader.Read(_br);
        //     while ((cursor = simpleBitStreamReader.GetNextOffset()) >= 0)
        //     {
        //         _data.m_Textures[cursor] = _br.ReadInt64();
        //     }
        // }
        // entities.Clear();
        // if (_version >= 4 && _version < 12)
        // {
        //     int num7 = _br.ReadInt16();
        //     for (int n = 0; n < num7; n++)
        //     {
        //         EntityCreationData entityCreationData = new EntityCreationData();
        //         entityCreationData.read(_br, _bNetworkRead: false);
        //         entities.Add(entityCreationData);
        //     }
        // }
        // if (_version >= 17)
        // {
        //     simpleBitStreamReader.Reset();
        //     simpleBitStreamReader.Read(_br);
        //     while ((cursor = simpleBitStreamReader.GetNextOffset()) >= 0)
        //     {
        //         _data.m_Water[cursor] = WaterValue.FromStream(_br);
        //     }
        // }
        // CellsFromArrays(ref _data);
        // if (_fixChildblocks)
        // {
        //     AddAllChildBlocks();
        // }
        // return true;
    }
}