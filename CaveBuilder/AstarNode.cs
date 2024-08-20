using System.Collections.Generic;

public class AstarNode
{
    public readonly Vector3i position;

    public AstarNode Parent { get; set; }

    public readonly int hashcode;

    public float GCost { get; set; }

    public float HCost { get; set; }

    public float FCost => GCost + HCost;

    public AstarNode(Vector3i pos)
    {
        position = pos;
        hashcode = position.GetHashCode();
    }

    public AstarNode(int x, int y, int z)
    {
        position = new Vector3i(x, y, z);
        hashcode = position.GetHashCode();
    }

    public override int GetHashCode()
    {
        return hashcode;
    }

    public override bool Equals(object obj)
    {
        AstarNode other = (AstarNode)obj;
        return hashcode == other.hashcode;
    }

    public List<CaveBlock> ReconstructPath()
    {
        var path = new List<CaveBlock>();
        var currentNode = this;

        while (currentNode != null)
        {
            path.Add(new CaveBlock(currentNode.position, MarchingCubes.DensityAir));
            currentNode = currentNode.Parent;
        }

        path.Reverse();

        return path;
    }
}
