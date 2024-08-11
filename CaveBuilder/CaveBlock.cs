using System.IO;

public struct CaveBlock
{
    public Vector2s ChunkPos { get; internal set; }

    public Vector3bf BlockPos { get; internal set; }

    public bool isWater;

    public bool isRope;

    public int x => (ChunkPos.x << 4) + BlockPos.x;

    public int y => BlockPos.y;

    public int z => (ChunkPos.z << 4) + BlockPos.z;

    public Vector3i position => new Vector3i(x, y, z);

    public CaveBlock(Vector3i position)
    {
        short chunk_x = (short)(position.x >> 4);
        short chunk_z = (short)(position.z >> 4);

        isWater = false;
        isRope = false;

        ChunkPos = new Vector2s(chunk_x, chunk_z);

        BlockPos = new Vector3bf(
            (byte)(position.x & 15),
            (byte)position.y,
            (byte)(position.z & 15)
        );
    }

    public CaveBlock(int x, int y, int z)
    {
        short chunk_x = (short)(x >> 4);
        short chunk_z = (short)(z >> 4);

        isWater = false;
        isRope = false;

        ChunkPos = new Vector2s(chunk_x, chunk_z);

        BlockPos = new Vector3bf(
            (byte)(x & 15),
            (byte)y,
            (byte)(z & 15)
        );
    }

    public CaveBlock(BinaryReader reader)
    {
        ChunkPos = new Vector2s(reader.ReadInt16(), reader.ReadInt16());
        BlockPos = new Vector3bf(reader.ReadUInt16());
        isWater = reader.ReadBoolean();
        isRope = reader.ReadBoolean();
    }

    public Vector3i ToVector3i()
    {
        return new Vector3i(x, y, z);
    }

    public void SetWater(bool isWater)
    {
        this.isWater = isWater;
    }

    public override int GetHashCode()
    {
        return ChunkPos.GetHashCode() + BlockPos.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is CaveBlock other)
        {
            return x == other.x && y == other.y && z == other.z;
        }
        return false;
    }

    public static bool operator ==(CaveBlock p1, CaveBlock p2)
    {
        return p1.x == p2.x && p1.y == p2.y && p1.z == p2.z;
    }

    public static bool operator !=(CaveBlock p1, CaveBlock p2)
    {
        return !(p1 == p2);
    }

    public override string ToString()
    {
        return $"{x},{y},{z}";
    }

    public void ToBinaryStream(BinaryWriter writer)
    {
        writer.Write(ChunkPos.x);
        writer.Write(ChunkPos.z);
        writer.Write(BlockPos.value);
        writer.Write(isWater);
        writer.Write(isRope);
    }
}
