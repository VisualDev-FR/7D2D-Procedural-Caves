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
    public void Test_CaveBlockNeighbors()
    {
        var block = new CaveBlock(10, 10, 10, MarchingCubes.DensityAir);

        foreach (var pos in CaveUtils.GetValidNeighbors(block.ToVector3i()))
        {
            Assert.IsTrue(pos.x == 9 || pos.x == 11 || pos.x == 10, $"{pos}");
            Assert.IsTrue(pos.y == 9 || pos.y == 11 || pos.y == 10, $"{pos}");
            Assert.IsTrue(pos.z == 9 || pos.z == 11 || pos.z == 10, $"{pos}");
        }
    }

    [TestMethod]
    public void Test_CaveBlockEquals()
    {
        var p1 = new CaveBlock(0, 1, 2, MarchingCubes.DensityAir);
        var p2 = new CaveBlock(0, 1, 2, MarchingCubes.DensityAir);
        Assert.AreEqual(p1, p2, $"{p1} | {p2}");

        p1 = new CaveBlock(0, 1, 2, MarchingCubes.DensityAir);
        p2 = new CaveBlock(0, 2, 2, MarchingCubes.DensityAir);
        Assert.AreNotEqual(p1, p2, $"{p1.BlockChunkPos}({p1.BlockChunkPos.GetHashCode()}) | {p2.BlockChunkPos}({p2.BlockChunkPos.GetHashCode()})");
    }

    public static void Test_prefabGrouping()
    {
        long memoryBefore = GC.GetTotalMemory(true);

        PrefabCache prefabCache = CaveBuilder.GetRandomPrefabs(CaveBuilder.PREFAB_COUNT);

        long memoryUsed = GC.GetTotalMemory(true) - memoryBefore;

        Log.Out($"Cave map size: {memoryUsed:N0} Bytes ({memoryUsed / 1_048_576.0:F1} MB)");
    }
}