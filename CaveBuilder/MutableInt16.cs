public class MutableInt16
{
    public short value;

    public MutableInt16(short value)
    {
        this.value = value;
    }

    public MutableInt16(int value)
    {
        this.value = (short)value;
    }

    public override int GetHashCode()
    {
        return value;
    }

    public override bool Equals(object obj)
    {
        if (obj is MutableInt16 other)
        {
            return other.value == value;
        }

        return false;
    }

    public override string ToString()
    {
        return value.ToString();
    }
}