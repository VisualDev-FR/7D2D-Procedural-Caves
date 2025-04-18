using System;
using System.Runtime.CompilerServices;

public class Bitfield
{
    private const short INLINE = (short)MethodImplOptions.AggressiveInlining;

    [MethodImpl(INLINE)]
    public static byte GetByte(int rawData, int offset)
    {
        return (byte)((rawData >> offset) & 0xFF);
    }

    [MethodImpl(INLINE)]
    public static void SetByte(ref int rawData, byte value, int offset)
    {
        rawData = (rawData & ~(0xFF << offset)) | (value << offset);
    }

    [MethodImpl(INLINE)]
    public static bool GetBit(byte rawData, int offset)
    {
        return (rawData & (1 << offset)) != 0;
    }

    [MethodImpl(INLINE)]
    public static void SetBit(ref byte rawData, bool value, int offset)
    {
        int mask = 1 << offset;
        rawData = (byte)(value ? (rawData | mask) : (rawData & ~mask));
    }

    public static string ToBinaryString(int value, int bits = 32)
    {
        return Convert.ToString(value, 2).PadLeft(bits, '0');
    }

    public static string ToHexString(int value, int bits = 8)
    {
        return value.ToString($"x{bits}");
    }

}