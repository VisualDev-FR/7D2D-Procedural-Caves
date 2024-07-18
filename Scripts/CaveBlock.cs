public struct CaveBlock
{
    public short x;
    public short y;
    public short z;
    public byte type;

    public CaveBlock(short x, short y, short z, byte type)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.type = type;
    }

    public override string ToString()
    {
        return $"{x},{y},{z}";
    }

    public static Vector3i operator +(CaveBlock block1, Vector3i block2)
    {
        return new Vector3i(
            block1.x + block2.x,
            block1.y + block2.y,
            block1.z + block2.z
        );
    }

    public static Vector3i operator -(CaveBlock block1, Vector3i block2)
    {
        return new Vector3i(
            block1.x - block2.x,
            block1.y - block2.y,
            block1.z - block2.z
        );
    }
}