public struct Segment
{
    public Vector3i P1;
    public Vector3i P2;

    public Segment(Vector3i p1, Vector3i p2)
    {
        P1 = p1;
        P2 = p2;
    }

    public Segment(int x0, int z0, int x1, int z1)
    {
        P1 = new Vector3i(x0, 0, z0);
        P2 = new Vector3i(x1, 0, z1);
    }

    public bool Intersect(Segment other)
    {
        Vector3i p1 = P1, q1 = P2;
        Vector3i p2 = other.P1, q2 = other.P2;

        int o1 = Orientation(p1, q1, p2);
        int o2 = Orientation(p1, q1, q2);
        int o3 = Orientation(p2, q2, p1);
        int o4 = Orientation(p2, q2, q1);

        if (o1 != o2 && o3 != o4)
            return true;

        if (o1 == 0 && OnSegment(p1, p2, q1))
            return true;

        if (o2 == 0 && OnSegment(p1, q2, q1))
            return true;

        if (o3 == 0 && OnSegment(p2, p1, q2))
            return true;

        if (o4 == 0 && OnSegment(p2, q1, q2))
            return true;

        return false;
    }

    private static int Orientation(Vector3i p, Vector3i q, Vector3i r)
    {
        double val = (q.z - p.z) * (r.x - q.x) - (q.x - p.x) * (r.z - q.z);

        if (val == 0)
            return 0;

        return (val > 0) ? 1 : 2;
    }

    private static bool OnSegment(Vector3i p, Vector3i q, Vector3i r)
    {
        if (q.x <= CaveUtils.FastMax(p.x, r.x) && q.x >= CaveUtils.FastMin(p.x, r.x) &&
            q.z <= CaveUtils.FastMax(p.z, r.z) && q.z >= CaveUtils.FastMin(p.z, r.z))
            return true;

        return false;
    }
}
