using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

// TODO: see https://github.com/giladfrid009/SimpleSIMD/tree/master

public class EuclidianSIMD
{
    // https://github.com/CBGonzalez/SIMDPerformance/tree/master

    public static Vector3i currentPosition;

    public static Vector3i goalPosition;

    public const int SIZE = 24;

    public const int NUM_VECTORS = 3;

    public static int[] neighbors_x = new int[SIZE];

    public static int[] neighbors_y = new int[SIZE];

    public static int[] neighbors_z = new int[SIZE];

    public static int[] current_x = new int[SIZE];

    public static int[] current_y = new int[SIZE];

    public static int[] current_z = new int[SIZE];

    public static int[] currentDistances = new int[SIZE];

    public static int[] goalDistances = new int[SIZE];

    public static int[] offsets_x = new int[] { 1, -1, 0, 0, 0, 0, 0, 0, 1, -1, 1, -1, 1, 1, -1, -1, 1, 1, 1, 1, -1, -1, -1, -1 };

    public static int[] offsets_y = new int[] { 0, 0, 0, 0, 1, -1, 1, -1, 0, 0, 0, 0, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1 };

    public static int[] offsets_z = new int[] { 0, 0, 1, -1, 1, 1, -1, -1, 1, 1, -1, -1, 0, 0, 0, 0, 1, 1, -1, -1, 1, 1, -1, -1 };

    public static void SIMDVectorsNoCopy(Vector3i _currentPosition, Vector3i _goalPosition)
    {
        currentPosition = _currentPosition;
        goalPosition = _goalPosition;

        for (int i = 0; i < SIZE; i++)
        {
            current_x[i] = currentPosition.x;
            current_y[i] = currentPosition.y;
            current_z[i] = currentPosition.z;
        }

        var offsetMemory_x = new ReadOnlyMemory<int>(offsets_x);
        var offsetMemory_y = new ReadOnlyMemory<int>(offsets_y);
        var offsetMemory_z = new ReadOnlyMemory<int>(offsets_z);

        var currentMemory_x = new ReadOnlyMemory<int>(current_x);
        var currentMemory_y = new ReadOnlyMemory<int>(current_y);
        var currentMemory_z = new ReadOnlyMemory<int>(current_z);

        var neighborsMemory_x = new Memory<int>(neighbors_x);
        var neighborsMemory_y = new Memory<int>(neighbors_y);
        var neighborsMemory_z = new Memory<int>(neighbors_z);

        ReadOnlySpan<Vector<int>> offsetSpan_x = MemoryMarshal.Cast<int, Vector<int>>(offsetMemory_x.Span);
        ReadOnlySpan<Vector<int>> offsetSpan_y = MemoryMarshal.Cast<int, Vector<int>>(offsetMemory_y.Span);
        ReadOnlySpan<Vector<int>> offsetSpan_z = MemoryMarshal.Cast<int, Vector<int>>(offsetMemory_z.Span);

        ReadOnlySpan<Vector<int>> currentSpan_x = MemoryMarshal.Cast<int, Vector<int>>(currentMemory_x.Span);
        ReadOnlySpan<Vector<int>> currentSpan_y = MemoryMarshal.Cast<int, Vector<int>>(currentMemory_y.Span);
        ReadOnlySpan<Vector<int>> currentSpan_z = MemoryMarshal.Cast<int, Vector<int>>(currentMemory_z.Span);

        Span<Vector<int>> neighborSpan_x = MemoryMarshal.Cast<int, Vector<int>>(neighborsMemory_x.Span);
        Span<Vector<int>> neighborSpan_y = MemoryMarshal.Cast<int, Vector<int>>(neighborsMemory_y.Span);
        Span<Vector<int>> neighborSpan_z = MemoryMarshal.Cast<int, Vector<int>>(neighborsMemory_z.Span);

        for (int i = 0; i < NUM_VECTORS; i++)
        {
            var neighbor_x = offsetSpan_x[i] + currentSpan_x[i];
            var neighbor_y = offsetSpan_y[i] + currentSpan_y[i];
            var neighbor_z = offsetSpan_z[i] + currentSpan_z[i];

            // neighborSpan_x[i] = offsetSpan_x[i] + currentSpan_x[i];
            // neighborSpan_y[i] = offsetSpan_y[i] + currentSpan_y[i];
            // neighborSpan_z[i] = offsetSpan_z[i] + currentSpan_z[i];
        }
    }

    public static void Demo(string[] args)
    {
        var current = new Vector3i(10, 10, 10);
        var goal = new Vector3i(20, 20, 20);

        SIMDVectorsNoCopy(current, goal);

        for (int i = 0; i < SIZE; i++)
        {
            Log.Out($"{neighbors_x[i]}, {neighbors_y[i]}, {neighbors_z[i]}");
        }
    }
}