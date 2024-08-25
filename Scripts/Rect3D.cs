public class Rect3D
{
    public Vector3i start;

    public Vector3i end;

    public Vector3i Size => end - start;

    public Rect3D(Vector3i start, Vector3i end)
    {
        this.start = start;
        this.end = end;
    }

    public override int GetHashCode()
    {
        return start.GetHashCode() + end.GetHashCode();
    }

    public Vector3i RotateCoords(Vector3i coord, int rotation, Vector3i parentSize)
    {
        var _x = coord.x;
        var _z = coord.z;

        switch (rotation)
        {
            case 3:
                {
                    int num = _x;
                    _x = _z;
                    _z = parentSize.x - num - 1;
                    break;
                }
            case 2:
                _x = parentSize.x - _x - 1;
                _z = parentSize.z - _z - 1;
                break;
            case 1:
                {
                    int num = _x;
                    _x = parentSize.z - _z - 1;
                    _z = num;
                    break;
                }
        }

        return new Vector3i(_x, coord.y, _z);
    }

    public Rect3D Transform(Vector3i position, byte rotation, Vector3i parentSize)
    {
        var start = RotateCoords(this.start, rotation, parentSize) + position;
        var end = RotateCoords(this.end, rotation, parentSize) + position;

        return new Rect3D(start, end);
    }
}
