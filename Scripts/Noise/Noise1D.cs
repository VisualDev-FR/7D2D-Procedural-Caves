using System;
using System.Collections.Generic;
using System.Numerics;


public class Noise1D
{
    public readonly List<Vector2> points;

    public Noise1D(Random rand, int r1, int r2, int distance)
    {
        float x0 = 0;
        float y0 = r1;
        float x1 = distance;
        float y1 = r2;

        int maxRadius = Utils.FastMax(r1, r2);
        int minRadius = 2;

        points = new List<Vector2>() {
            new Vector2(x0, y0),
            new Vector2(x1, y1),
        };

        for (int i = 0; i < 4; i++)
        {
            float x = distance * (float)rand.NextDouble();
            float y = minRadius + (maxRadius - minRadius) * (float)rand.NextDouble();

            points.Add(new Vector2(x, y));
        }

        points.Sort((a, b) => a.X.CompareTo(b.X));
    }

    public int Interpolate(int index)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            if (index >= points[i].X && index <= points[i + 1].X)
            {
                double x0 = points[i].X;
                double y0 = points[i].Y;
                double x1 = points[i + 1].X;
                double y1 = points[i + 1].Y;

                return (int)(y0 + (y1 - y0) * (index - x0) / (x1 - x0));
            }
        }

        throw new Exception($"Interpolation failed for index: '{index}'");
    }
}
