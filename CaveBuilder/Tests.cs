using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class GraphNodeTests
{

    [TestMethod]
    public void Test_nodeDirection()
    {
        var prefab = new CavePrefab(0)
        {
            position = new Vector3i(0, 0, 0),
            Size = new Vector3i(10, 10, 10),
        };

        var node1 = new GraphNode(new Vector3i(-1, 0, 5), prefab);
        var node2 = new GraphNode(new Vector3i(5, 0, 10), prefab);
        var node3 = new GraphNode(new Vector3i(10, 0, 7), prefab);
        var node4 = new GraphNode(new Vector3i(1, 0, -1), prefab);

        Log.Out(node1.direction.Vector.ToString());

        Assert.AreEqual(Direction.North, node1.direction);
        Assert.AreEqual(Direction.East, node2.direction);
        Assert.AreEqual(Direction.South, node3.direction);
        Assert.AreEqual(Direction.West, node4.direction);
    }

    [TestMethod]
    public void Test_DirectionEquals()
    {
        var dir1 = new Direction(0, 1);
        var dir2 = new Direction(0, 1);

        Assert.IsTrue(dir1 == dir2);
        Assert.AreEqual(dir1, dir2);
    }

    [TestMethod]
    public void Test_CaveBlockEquals()
    {
        var p1 = new CaveBlock(0, 1, 2);
        var p2 = new CaveBlock(0, 1, 2);
        Assert.AreEqual(p1, p2, $"{p1} | {p2}");

        p1 = new CaveBlock(0, 1, 2);
        p2 = new CaveBlock(0, 2, 2);
        Assert.AreNotEqual(p1, p2, $"{p1.blockChunkPos}({p1.blockChunkPos.GetHashCode()}) | {p2.blockChunkPos}({p2.blockChunkPos.GetHashCode()})");
    }

}