public struct Vector2s
{
    public short x;

    public short z;

    public Vector2s(short x, short z)
    {
        this.x = x;
        this.z = z;
    }

    public Vector2s(int x, int z)
    {
        this.x = checked((short)x);
        this.z = checked((short)z);
    }

    public Vector2s(Vector3i other)
    {
        x = (short)other.x;
        z = (short)other.z;
    }

    public Vector2s(string value)
    {
        var array = value.Split(',');

        x = short.Parse(array[0]);
        z = short.Parse(array[1]);
    }

    public override string ToString()
    {
        return $"{x},{z}";
    }

    public override bool Equals(object obj)
    {
        if (obj is Vector2s other)
        {
            return x == other.x && z == other.z;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return x ^ (z << 2);
    }
}