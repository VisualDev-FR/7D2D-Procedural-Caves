using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class CmdGraphDebug : CmdAbstract
{
    public override string[] GetCommands()
    {
        throw new System.NotImplementedException();
    }

    public override void Execute(List<string> args)
    {
        var filename = "ignore/graph.txt";
        var worldSize = 0;

        var Prefabs = new List<CavePrefab>();
        var Edges = new List<GraphEdge>();

        using (var reader = new StreamReader(filename))
        {
            worldSize = int.Parse(reader.ReadLine());

            int prefabCount = int.Parse(reader.ReadLine());

            Logging.Info("prefabCount: " + prefabCount.ToString());
            for (int i = 0; i < prefabCount; i++)
            {
                var start = new Vector3i(
                    int.Parse(reader.ReadLine()),
                    0,
                    int.Parse(reader.ReadLine())
                );

                var size = new Vector3i(
                    int.Parse(reader.ReadLine()),
                    0,
                    int.Parse(reader.ReadLine())
                );
                Prefabs.Add(new CavePrefab()
                {
                    position = start,
                    Size = size,
                });
            }

            int edgesCount = int.Parse(reader.ReadLine());

            Logging.Info("edgesCount: " + edgesCount.ToString());

            for (int i = 0; i < edgesCount; i++)
            {
                var start = new Vector3i(
                    int.Parse(reader.ReadLine()),
                    0,
                    int.Parse(reader.ReadLine())
                );

                var size = new Vector3i(
                    int.Parse(reader.ReadLine()),
                    0,
                    int.Parse(reader.ReadLine())
                );

                var node1 = new GraphNode(start);
                var node2 = new GraphNode(size);

                Edges.Add(new GraphEdge(node1, node2));
            }
        }

        using (var b = new Bitmap(worldSize, worldSize))
        {
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(DrawingUtils.BackgroundColor);
                DrawingUtils.DrawEdges(g, Edges);
                DrawingUtils.DrawPrefabs(b, g, Prefabs);
            }

            b.Save(@"graph.png", ImageFormat.Png);
        }
    }

}