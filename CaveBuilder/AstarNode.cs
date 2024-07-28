using System.Collections.Generic;

public class AstarNode
{
    public Vector3i position;

    public float GCost { get; set; } // coût du chemin depuis le noeud de départ jusqu'à ce noeud

    public float HCost { get; set; } // estimation heuristique du coût restant jusqu'à l'objectif

    public float FCost => GCost + HCost; // coût total estimé (GCost + HCost)

    public AstarNode Parent { get; set; } // noeud parent dans le chemin optimal

    public AstarNode(Vector3i pos)
    {
        position = pos;
    }

    public AstarNode(int x, int y, int z)
    {
        position = new Vector3i(x, y, z);
    }

    public List<AstarNode> GetNeighbors()
    {
        var neighbors = CaveUtils.GetValidNeighbors(position);
        var nodes = new List<AstarNode>();

        foreach (var pos in neighbors)
        {
            if (pos.x == position.x && pos.z == position.z && pos.y != position.y)
                continue;

            nodes.Add(new AstarNode(pos));
        }

        return nodes;
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        AstarNode other = (AstarNode)obj;
        return position.GetHashCode() == other.position.GetHashCode();
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
