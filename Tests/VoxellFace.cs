public struct VoxellFace
{
    public int[] vertIndices;

    public VoxellFace(int[] values)
    {
        vertIndices = values;
    }

    public VoxellFace(int a, int b, int c, int d)
    {
        vertIndices = new int[4] { a, b, c, d };
    }

    public override string ToString()
    {
        return $"f {vertIndices[0]} {vertIndices[1]} {vertIndices[2]} {vertIndices[3]}";
    }
}

