#pragma warning disable IDE1006

using System;
using UnityEngine;

public struct Vector3bf
{
    // NOTE:
    // Allow to save memory size of a relative chunk position by storing only a unique unsigned short bitfield
    // sizes: [0 < x, z < 16], [0 < y < 256]
    // allow to store 2x 4-bits (x, z) + 1x 8-bits (y) in one unique unsigned short
    // uses only 16 bits, instead of using 24 bits by storing 3 bytes (only for memory optimization purpose)
    // wil be used to store the relative positions between a chunk and a caveBlock

    private ushort _value;

    public ushort value => _value;

    public byte x
    {
        get => (byte)(_value & 0b0000_0000_0000_1111);
        set => _value = (ushort)((this._value & 0b1111_0000_0000_0000) | (value & 0b0000_0000_0000_1111));
    }

    public byte y
    {
        get => (byte)((_value >> 4) & 0b0000_0000_1111_1111);
        set => _value = (ushort)((this._value & 0b1111_0000_0000_1111) | ((value & 0b0000_0000_1111_1111) << 4));
    }

    public byte z
    {
        get => (byte)((_value >> 12) & 0b0000_0000_0000_1111);
        set => _value = (ushort)((this._value & 0b0000_1111_1111_1111) | ((value & 0b0000_0000_0000_1111) << 12));
    }

    public Vector3bf(byte _x, byte _y, byte _z)
    {
        _value = 0;
        x = _x;
        y = _y;
        z = _z;
    }

    public Vector3bf(ushort value)
    {
        _value = value;
    }

    public Vector3bf(string value)
    {
        this._value = 0;

        string[] array = value.Split(',');

        x = byte.Parse(array[0]);
        y = byte.Parse(array[1]);
        z = byte.Parse(array[2]);
    }

    public override string ToString()
    {
        return $"{x},{y},{z}";
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    public Vector3i ToVector3i()
    {
        return new Vector3i(x, y, z);
    }

    public string ToBinaryString()
    {
        string binaryString = Convert.ToString(_value, 2).PadLeft(16, '0');

        binaryString = binaryString.Insert(4, "|");
        binaryString = binaryString.Insert(13, "|");

        return binaryString;
    }

    public override int GetHashCode()
    {
        return _value;
    }

    public override bool Equals(object obj)
    {
        if (obj is Vector3bf other)
        {
            return _value == other._value;
        }
        return false;
    }
}