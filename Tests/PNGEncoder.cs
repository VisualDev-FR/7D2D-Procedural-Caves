using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class PNGEncoder
{
    public Color32[] pixels;

    public int worldSize;

    public PNGEncoder(int _worldSize)
    {
        worldSize = _worldSize;
        pixels = new Color32[worldSize * worldSize];
    }

    public void SetPixel(int x, int z, Color32 color)
    {
        pixels[x * worldSize + z] = color;
    }

    public void DrawLine(Vector3i p1, Vector3i p2, Color32 color)
    {
        int x0 = p1.x;
        int y0 = p1.y;
        int x1 = p2.x;
        int y1 = p2.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            SetPixel(x0, y0, color);

            if (x0 == x1 && y0 == y1) break;

            int e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

    }

    public void DrawGrid(int gridSize, Color32 color)
    {

    }

    public void DrawRectangle(Vector3i start, Vector3i size, Color32 color)
    {

    }

    public void Encode(string path)
    {
        File.WriteAllBytes(path, ImageConversion.EncodeArrayToPNG(
            pixels,
            GraphicsFormat.R8G8B8A8_UNorm,
            (uint)worldSize,
            (uint)worldSize,
            (uint)worldSize * 4
        ));
    }
}

