using System.Collections.Generic;

public class Voxell
{
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

    public Vector3i position;

    public Vector3i size = Vector3i.one;

    public bool force;

    public string material = "";

    public int[,] Vertices
    {
        get
        {
            int x = size.x;
            int y = size.y;
            int z = size.z;

            return new int[,]
            {
                {0, 0, 0},
                {x, 0, 0},
                {x, y, 0},
                {0, y, 0},
                {0, 0, z},
                {x, 0, z},
                {x, y, z},
                {0, y, z},
            };
        }
    }

    public static Dictionary<int, int[]> faceMapping = new Dictionary<int, int[]>()
    {
        { 0, new int[4]{ 1, 2, 6, 5 } }, // normal: x + 1
        { 1, new int[4]{ 3, 0, 4, 7 } }, // normal: x - 1
        { 2, new int[4]{ 2, 3, 7, 6 } }, // normal: y + 1
        { 3, new int[4]{ 0, 1, 5, 4 } }, // normal: y - 1
        { 4, new int[4]{ 4, 5, 6, 7 } }, // normal: z + 1
        { 5, new int[4]{ 0, 1, 2, 3 } }, // normal: z - 1
    };

    public Voxell(Vector3i position)
    {
        this.position = position;
    }

    public Voxell(Vector3i position, string material)
    {
        this.position = position;
        this.material = material;
    }

    public Voxell(Vector3i position, Vector3i size)
    {
        this.position = position;
        this.size = size;
    }

    public Voxell(Vector3i position, Vector3i size, string material)
    {
        this.position = position;
        this.size = size;
        this.material = material;
    }

    public Voxell(Vector3i position, int sizeX, int sizeY, int sizeZ)
    {
        this.position = position;
        size = new Vector3i(sizeX, sizeY, sizeZ);
    }

    public Voxell(int x, int y, int z)
    {
        position = new Vector3i(x, y, z);
    }

    public Voxell(int x, int y, int z, string material)
    {
        position = new Vector3i(x, y, z);
        this.material = material;
    }

    public bool[] GetNeighbors(HashSet<Voxell> others)
    {
        if (force)
            return new bool[6] { true, true, true, true, true, true };

        var result = new bool[6];
        var neighbors = new List<Voxell>()
        {
            new Voxell(position.x + 1, position.y, position.z),
            new Voxell(position.x - 1, position.y, position.z),
            new Voxell(position.x, position.y + 1, position.z),
            new Voxell(position.x, position.y - 1, position.z),
            new Voxell(position.x, position.y, position.z + 1),
            new Voxell(position.x, position.y, position.z - 1),
        };

        for (int i = 0; i < 6; i++)
        {
            result[i] = !others.Contains(neighbors[i]);
        }

        return result;
    }

    public bool ShouldAddVertice(int index, bool[] neighbors)
    {
        // 0 {0, 0, 0},
        // 1 {x, 0, 0},
        // 2 {x, y, 0},
        // 3 {0, y, 0},
        // 4 {0, 0, z},
        // 5 {x, 0, z},
        // 6 {x, y, z},
        // 7 {0, y, z},

        /*
            0 | 0 1 2 3 | z - 1
            1 | 4 5 6 7 | z + 1
            2 | 0 1 5 4 | y - 1
            3 | 1 2 6 5 | x + 1
            4 | 2 3 7 6 | y + 1
            5 | 3 0 4 7 | x - 1
        */

        if (index == 0)
            return neighbors[0] || neighbors[2] || neighbors[5];

        if (index == 1)
            return neighbors[0] || neighbors[2] || neighbors[3];

        if (index == 2)
            return neighbors[0] || neighbors[3] || neighbors[4];

        if (index == 3)
            return neighbors[0] || neighbors[4] || neighbors[5];

        if (index == 4)
            return neighbors[1] || neighbors[4] || neighbors[5];

        if (index == 5)
            return neighbors[1] || neighbors[2] || neighbors[3];

        if (index == 6)
            return neighbors[1] || neighbors[3] || neighbors[4];

        if (index == 7)
            return neighbors[1] || neighbors[4] || neighbors[5];

        return false;
    }

    public string ToWavefront(ref int vertexIndexOffset, HashSet<Voxell> others)
    {
        var strVertices = new List<string>();

        for (int i = 0; i < 8; i++)
        {
            int vx = Vertices[i, 0] + position.x;
            int vy = Vertices[i, 1] + position.y;
            int vz = Vertices[i, 2] + position.z;

            strVertices.Add($"v {vx} {vy} {vz}");
        }

        var faces = new List<VoxellFace>();
        var usedVerticeIndexes = new Dictionary<int, int>();
        var neighbors = GetNeighbors(others);
        var result = new List<string>();

        foreach (var entry in faceMapping)
        {
            int faceIndex = entry.Key;
            int[] verticeIndexes = entry.Value;

            if (!neighbors[faceIndex])
                continue;

            for (int i = 0; i < 4; i++)
            {
                int index = verticeIndexes[i];

                if (!usedVerticeIndexes.ContainsKey(index))
                {
                    result.Add(strVertices[index] + $" # {vertexIndexOffset + 1}");
                    usedVerticeIndexes[index] = ++vertexIndexOffset;
                }
            }

            faces.Add(new VoxellFace(
                usedVerticeIndexes[verticeIndexes[0]],
                usedVerticeIndexes[verticeIndexes[1]],
                usedVerticeIndexes[verticeIndexes[2]],
                usedVerticeIndexes[verticeIndexes[3]]
            ));
        }

        if (faces.Count == 0)
            return "";

        if (material != "")
            result.Add($"usemtl {material}");

        foreach (var face in faces)
        {
            result.Add(face.ToString());
        }

        return string.Join("\n", result);
    }

    public override int GetHashCode()
    {
        return position.GetHashCode() + size.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        var other = (Voxell)obj;

        return GetHashCode() == other.GetHashCode();
    }

}
