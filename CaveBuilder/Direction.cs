public class Direction
{
    public static Direction North = new Direction(-1, 0);

    public static Direction South = new Direction(1, 0);

    public static Direction East = new Direction(0, 1);

    public static Direction West = new Direction(0, -1);

    public static Direction None = new Direction(0, 0);

    public Vector3i Vector { get; internal set; }

    public Direction(int x, int z)
    {
        Vector = new Vector3i(x, 0, z);
    }

    public override bool Equals(object obj)
    {
        Direction other = (Direction)obj;
        return Vector.GetHashCode() == other.GetHashCode();
    }

    public override int GetHashCode()
    {
        return Vector.GetHashCode();
    }

    public static bool operator ==(Direction dir1, Direction dir2)
    {
        return dir1.Vector == dir2.Vector;
    }

    public static bool operator !=(Direction dir1, Direction dir2)
    {
        return dir1.Vector != dir2.Vector;
    }
}
