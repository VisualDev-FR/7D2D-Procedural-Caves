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
            size = new Vector3i(10, 10, 10),
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
}