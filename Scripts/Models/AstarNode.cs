using System.Collections.Generic;

public class AstarNode
{
    public AstarNode Parent { get; private set; }

    public readonly Vector3i position;
    public readonly int hashcode;

    public readonly int totalDist = 0;

    public float GCost { get; set; }

    public float HCost { get; set; }

    public float FCost => GCost + HCost;
    public AstarNode(Vector3i pos, AstarNode parent = null)
    {
        Parent = parent;
        position = pos;
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        AstarNode other = (AstarNode)obj;
        return GetHashCode() == other.GetHashCode();
    }

    public List<CaveBlock> ReconstructPath()
    {
        var path = new List<CaveBlock>();
        var currentNode = this;

        while (currentNode != null)
        {
            path.Add(new CaveBlock(currentNode.position));
            currentNode = currentNode.Parent;
        }

        path.Reverse();

        return path;
    }

    public float SqrEuclidianDist(AstarNode other)
    {
        return CaveUtils.SqrEuclidianDist(position, other.position);
    }
}
