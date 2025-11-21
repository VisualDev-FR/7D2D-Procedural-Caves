using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using BumpKit;

public class DrawingUtils
{
    public static readonly Color BackgroundColor = Color.Black;

    public static readonly Color TunnelsColor = Color.DarkRed;

    public static readonly Color NodeColor = Color.Yellow;

    public static readonly Color UnderGroundColor = Color.Green;

    public static readonly Color EntranceColor = Color.Yellow;

    public static readonly Color RoomColor = Color.DarkGray;

    public static readonly Color NoiseColor = Color.FromArgb(84, 84, 82);

    public static PointF ParsePointF(Vector3i point)
    {
        return new PointF(point.x, point.z);
    }

    public static void DrawGrid(Bitmap b, Graphics graph, int worldSize, int gridSize)
    {
        for (int x = gridSize; x < worldSize; x += gridSize)
        {
            var color = Color.FromArgb(50, Color.FromName("DarkGray"));

            using (var pen = new Pen(color, 1))
            {
                graph.DrawLine(pen, new PointF(x, 0), new PointF(x, worldSize));
                graph.DrawLine(pen, new PointF(0, x), new PointF(worldSize, x));
            }
        }
    }

    public static void DrawPrefabs(Bitmap b, Graphics graph, List<CavePrefab> prefabs)
    {
        foreach (var prefab in prefabs)
        {
            // var baseColor = Color.FromArgb(prefab.Name.GetHashCode());
            // var color = Color.FromArgb(255, baseColor);

            var color = UnderGroundColor;

            if (prefab.isRoom)
            {
                color = RoomColor;
            }
            else if (prefab.isEntrance)
            {
                color = EntranceColor;
            }

            using (var pen = new Pen(color, 1))
            {
                graph.DrawRectangle(pen, prefab.position.x, prefab.position.z, prefab.Size.x, prefab.Size.z);
            }

            if (prefab.nodes is null)
                continue;

            foreach (var node in prefab.nodes)
            {
                b.SetPixel(node.position.x, node.position.z, NodeColor);
            }
        }
    }

    public static void DrawPoints(Bitmap bitmap, IEnumerable<Vector3i> points, Color color)
    {
        foreach (var point in points)
        {
            bitmap.SetPixel(point.x, point.z, color);
        }
    }

    public static void DrawEdges(Graphics graph, List<GraphEdge> edges)
    {
        foreach (var edge in edges)
        {
            // var color = edge.isVirtual ? RoomColor : TunnelsColor;
            var color = Color.FromName(edge.colorName);

            using (var pen = new Pen(Color.FromArgb(edge.opacity, color), edge.width))
            {
                graph.DrawCurve(pen, new PointF[2]{
                    ParsePointF(edge.node1.position),
                    ParsePointF(edge.node2.position),
                });
            }
        }
    }

    public static void DrawNoise(Bitmap b, CaveNoise noise, int worldSize)
    {
        for (int x = 0; x < worldSize; x++)
        {
            for (int z = 0; z < worldSize; z++)
            {
                if (noise.IsCave(x, z))
                    b.SetPixel(x, z, NoiseColor);
            }
        }
    }

    public static void HexToRgb(string[] args)
    {
        var hexColor = args[1];

        if (hexColor.StartsWith("#"))
        {
            hexColor = hexColor.Substring(1);
        }

        if (hexColor.Length != 6)
        {
            throw new ArgumentException("La couleur hexadécimale doit être de 6 caractères.");
        }

        float r = 1f * Convert.ToInt32(hexColor.Substring(0, 2), 16) / 255;
        float g = 1f * Convert.ToInt32(hexColor.Substring(2, 2), 16) / 255;
        float b = 1f * Convert.ToInt32(hexColor.Substring(4, 2), 16) / 255;

        Console.WriteLine($"Kd {r:F2} {g:F2} {b:F2}".Replace(",", "."));
    }

    public static void GenerateObjFile(string filename, HashSet<Voxell> voxels, bool openFile = false)
    {
        int index = 0;

        using (StreamWriter writer = new StreamWriter(filename))
        {
            writer.WriteLine("mtllib materials.mtl");

            foreach (var voxel in voxels)
            {
                string strVoxel = voxel.ToWavefront(ref index, voxels);

                if (strVoxel != "")
                    writer.WriteLine(strVoxel);
            }
        }

        if (openFile)
        {
            Process.Start("CMD.exe", $"/C {Path.GetFullPath(filename)}");
        }
    }

    public static Color InterpolateColor(Color start, Color end, float factor)
    {
        int r = (int)(start.R + (end.R - start.R) * factor);
        int g = (int)(start.G + (end.G - start.G) * factor);
        int b = (int)(start.B + (end.B - start.B) * factor);
        int a = (int)(start.A + (end.A - start.A) * factor);

        return Color.FromArgb(a, r, g, b);
    }

    public static void EncodeGif(string outputFilePath, string[] imageFilePaths, int delay = 1000)
    {
        using (var stream = new FileStream(outputFilePath, FileMode.OpenOrCreate))
        {
            using (var e = new GifEncoder(stream))
            {
                e.FrameDelay = new TimeSpan(0, 0, 0, 0, delay);

                foreach (var path in imageFilePaths)
                {
                    e.AddFrame(Image.FromFile(path));
                }
            }
        }
    }

}