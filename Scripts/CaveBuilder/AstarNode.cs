using System;
using System.Collections.Generic;

public class AstarNode
{
    public AstarNode Parent { get; private set; }

    public readonly Vector3i position;

    public readonly Vector3i direction;

    public readonly int hashcode;

    public readonly int totalDist = 0;

    public float GCost { get; set; }

    public float HCost { get; set; }

    public float FCost => GCost + HCost;

    public AstarNode(Vector3i pos, Vector3i parent)
    {
        Parent = null;
        position = pos;
        hashcode = position.GetHashCode();
        direction.x = Math.Sign(pos.x - parent.x);
        direction.y = Math.Sign(pos.y - parent.y);
        direction.z = Math.Sign(pos.z - parent.z);
    }

    public AstarNode(Vector3i pos, AstarNode parent)
    {
        totalDist = parent.totalDist + 1;
        Parent = parent;
        position = pos;
        hashcode = position.GetHashCode();
        direction.x = Math.Sign(pos.x - parent.position.x);
        direction.y = Math.Sign(pos.y - parent.position.y);
        direction.z = Math.Sign(pos.z - parent.position.z);
    }

    public AstarNode(Vector3i pos)
    {
        position = pos;
        hashcode = pos.GetHashCode();
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
