public class PlaneEquation
{
    private int A, B, C, D;

    public PlaneEquation(Vector3i p1, Vector3i p2, Vector3i p3)
    {
        var v1 = new Vector3i(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z);
        var v2 = new Vector3i(p3.x - p1.x, p3.y - p1.y, p3.z - p1.z);

        A = v1.y * v2.z - v1.z * v2.y;
        B = v1.z * v2.x - v1.x * v2.z;
        C = v1.x * v2.y - v1.y * v2.x;
        D = -(A * p1.x + B * p1.y + C * p1.z);
    }

    public int GetHeight(int x, int z)
    {
        return -(A * x + C * z + D) / B;
    }
}
