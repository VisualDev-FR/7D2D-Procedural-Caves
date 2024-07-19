#pragma warning disable IDE1006

using System;

public struct Vector3bf
{
    // NOTE:
    // wrapper for storing Vector3 with x, y, and z all stored in a unique 16 bits sized bitfield
    // allow to store 2x 4-bits (x, z) + 1x 8-bits (y) in one unique unsigned short
    // uses only 16 bits, instead of using 24 bits by storing 3 bytes (only for memory optimization purpose)
    // wil be used to store the relative positions between a chunk and a caveBlock

    private ushort data;

    public byte x
    {
        get => (byte)(data & 0b0000_0000_0000_1111);
        set => data = (ushort)((data & 0b1111_0000_0000_0000) | (value & 0b0000_0000_0000_1111));
    }

    public byte y
    {
        get => (byte)((data >> 4) & 0b0000_0000_1111_1111);
        set => data = (ushort)((data & 0b1111_0000_0000_1111) | ((value & 0b0000_0000_1111_1111) << 4));
    }

    public byte z
    {
        get => (byte)((data >> 12) & 0b0000_0000_0000_1111);
        set => data = (ushort)((data & 0b0000_1111_1111_1111) | ((value & 0b0000_0000_0000_1111) << 12));
    }

    public Vector3bf(byte _x, byte _y, byte _z)
    {
        data = 0;
        x = _x;
        y = _y;
        z = _z;
    }

    public Vector3bf(string value)
    {
        data = 0;

        string[] array = value.Split(',');

        x = byte.Parse(array[0]);
        y = byte.Parse(array[1]);
        z = byte.Parse(array[2]);
    }

    public override string ToString()
    {
        return $"{x},{y},{z}";
    }

    public string ToBinaryString()
    {
        string binaryString = Convert.ToString(data, 2).PadLeft(16, '0');

        binaryString = binaryString.Insert(4, "|");
        binaryString = binaryString.Insert(13, "|");

        return binaryString;
    }
}