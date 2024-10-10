using System;
using System.Collections.Generic;

public class AstarNode
{
    public readonly Vector3i position;

    public AstarNode Parent { get; private set; }

    public Vector3i direction;

    public readonly int hashcode;

    public float GCost { get; set; }

    public float HCost { get; set; }

    public float FCost => GCost + HCost;

    public AstarNode(Vector3i pos, Vector3i parent)
    {
        Parent = null;
        position = pos;
        hashcode = position.GetHashCode();
        direction = new Vector3i(
            Math.Sign(pos.x - parent.x),
            Math.Sign(pos.y - parent.y),
            Math.Sign(pos.z - parent.z)
        );
    }

    public AstarNode(Vector3i pos, AstarNode parent)
    {
        Parent = parent;
        position = pos;
        hashcode = position.GetHashCode();
        direction = new Vector3i(
            Math.Sign(pos.x - parent.position.x),
            Math.Sign(pos.y - parent.position.y),
            Math.Sign(pos.z - parent.position.z)
        );
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
            path.Add(new CaveBlock(currentNode.position));
            currentNode = currentNode.Parent;
        }

        path.Reverse();

        return path;
    }
}
