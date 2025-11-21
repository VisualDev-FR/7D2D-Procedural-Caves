using System.Collections.Generic;


public struct RLELayer
{
    /*
        rawData      : 0000 0000 0000 0000 0000 0000 0000 0000
        BlockRawData :                               ^^^^ ^^^^
        end          :                     ^^^^ ^^^^
        start        :           ^^^^ ^^^^
    */
    public int rawData;

    public byte Start
    {
        get => Bitfield.GetByte(rawData, 16);
        set => Bitfield.SetByte(ref rawData, value, 16);
    }

    public byte End
    {
        get => Bitfield.GetByte(rawData, 8);
        set => Bitfield.SetByte(ref rawData, value, 8);
    }

    public byte BlockRawData
    {
        get => Bitfield.GetByte(rawData, 0);
        set => Bitfield.SetByte(ref rawData, value, 0);
    }

    public int Size => End - Start;

    public static int Count(int hash)
    {
        var layer = new RLELayer(hash);
        return layer.End - layer.Start;
    }

    public RLELayer(int start, int end, byte blockRawData)
    {
        rawData = (start << 16) | (end << 8) | blockRawData;
    }

    public RLELayer(IEnumerable<Vector3i> positions, byte blockRawData)
    {
        var start = int.MaxValue;
        var end = int.MinValue;

        foreach (var pos in positions)
        {
            start = Utils.FastMin(start, pos.y);
            end = Utils.FastMax(end, pos.y);
        }

        rawData = (start << 16) | (end << 8) | blockRawData;
    }

    public RLELayer(int bitfield)
    {
        this.rawData = bitfield;
    }

    public static int GetHashCode(int start, int end, byte blockRawData)
    {
        return (start << 16) | (end << 8) | blockRawData;
    }

    public bool Contains(int y)
    {
        return y >= Start && y <= End;
    }

    public IEnumerable<CaveBlock> GetBlocks(int x, int z)
    {
        for (int y = Start; y <= End; y++)
        {
            yield return new CaveBlock(x, y, z) { rawData = BlockRawData };
        }
    }

    public bool IsWater()
    {
        return (BlockRawData & 0b0000_0001) != 0;
    }

    public void SetWater(bool value)
    {
        BlockRawData = (byte)(value ? (BlockRawData | 0b0000_0001) : (BlockRawData & 0b1111_1110));
    }

    public void SetRope(bool value)
    {
        BlockRawData = (byte)(value ? (BlockRawData | 0b0000_1000) : (BlockRawData & 0b1111_0111));
    }

    public override int GetHashCode()
    {
        return rawData;
    }

    public static List<int> CompressLayers(IEnumerable<int> layers)
    {
        var layer = new RLELayer();
        var values = new byte?[256];
        byte minSet = 255;
        byte maxSet = 0;

        foreach (var layerHash in layers)
        {
            layer.rawData = layerHash;

            minSet = minSet < layer.Start ? minSet : layer.Start;
            maxSet = maxSet > layer.End ? maxSet : layer.End;

            for (int i = layer.Start; i <= layer.End; i++)
            {
                if (values[i] is null)
                {
                    values[i] = layer.BlockRawData;
                }
                else if (values[i].Value != layer.BlockRawData)
                {
                    values[i] = (byte)(values[i].Value | layer.BlockRawData);
                }
            }
        }

        var result = new List<int>();

        byte? previousData = values[minSet].Value;
        int startY = minSet;

        for (int i = minSet + 1; i <= maxSet; i++)
        {
            if (values[i] != previousData)
            {
                if (previousData.HasValue)
                {
                    result.Add(GetHashCode(startY, i - 1, previousData.Value));
                }

                startY = i;
                previousData = values[i];
            }
        }

        if (previousData.HasValue)
        {
            result.Add(GetHashCode(startY, maxSet, previousData.Value));
        }

        return result;
    }

    public bool AssertEquals(byte start, byte end, byte data)
    {
        CaveUtils.Assert(this.Start == start, $"start, value: '{this.Start}', expected: '{start}'");
        CaveUtils.Assert(this.End == end, $"end, value: '{this.End}', expected: '{end}'");
        CaveUtils.Assert(this.BlockRawData == data, $"data, value: '{this.BlockRawData}', expected: '{data}'");

        return true;
    }

}
