
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class StalactiteGenerator
{
    private static readonly Random Rand = new Random();

    public static void Generate(Vector3i start)
    {
        Log.Error("not implemented, give a height to have a result.");
    }

    public static void Generate(Vector3i start, int height)
    {
        var end = new Vector3i(start.x, start.y + height, start.z);

        BlockValue? blockValue = CaveEditorConsoleCmd.GetSelectedItem(allowAir: false);

        if (blockValue.HasValue)
        {
            Generate(start, end, blockValue.Value);
        }
    }

    private static void Generate(Vector3i start, Vector3i end, BlockValue blockValue)
    {
        var primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();

        Block block = blockValue.Block;
        BlockPlacement.Result _bpResult = new BlockPlacement.Result(0, Vector3.zero, Vector3i.zero, blockValue);
        block.OnBlockPlaceBefore(GameManager.Instance.World, ref _bpResult, primaryPlayer, GameManager.Instance.World.GetGameRandom());
        blockValue = _bpResult.blockValue;

        List<BlockChangeInfo> list = new List<BlockChangeInfo>();

        int _density = -128;
        int totalHeight = (int)Utils.FastAbs(end.y - start.y);

        for (int y = start.y; y != end.y; y += Math.Sign(end.y - start.y))
        {
            var position = new Vector3i(start.x, y, start.z);

            _density = Rand.Next(_density, _density + 50);
            _density = Utils.FastMin(_density, -10);

            list.Add(new BlockChangeInfo(position, blockValue, (sbyte)_density));
        }

        GameManager.Instance.SetBlocksRPC(list);
    }
}