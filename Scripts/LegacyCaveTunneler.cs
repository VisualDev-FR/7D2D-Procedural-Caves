using System;
using System.Collections.Generic;
using UnityEngine;


public static class LegacyCaveSystem
{
    private static BlockValue caveAir = new BlockValue((uint)Block.GetBlockByName("air").blockID);

    private static BlockValue bottomCaveDecoration = new BlockValue((uint)Block.GetBlockByName("cntCaveFloorRandomLootHelper").blockID);

    private static BlockValue topCaveDecoration = new BlockValue((uint)Block.GetBlockByName("cntCaveCeilingRandomLootHelper").blockID);

    public static void AddDecorationsToCave(Chunk chunk)
    {
        if (chunk == null)
            return;

        var chunkPos = chunk.GetWorldPos();

        GameRandom random = Utils.RandomFromSeedOnPos(chunk.ChunkPos.x, chunk.ChunkPos.z, GameManager.Instance.World.Seed);

        for (var chunkX = 1; chunkX < 15; chunkX++)
        {
            for (var chunkZ = 1; chunkZ < 15; chunkZ++)
            {
                var worldX = chunkPos.x + chunkX;
                var worldZ = chunkPos.z + chunkZ;

                var tHeight = chunk.GetTerrainHeight(chunkX, chunkZ);

                // One test world, we blew through a threshold.
                if (tHeight > 250)
                    tHeight = 240;

                // Move from the bottom up, leaving the last few blocks untouched.
                //for (int y = tHeight; y > 5; y--)
                for (var chunkY = 5; chunkY < tHeight - 2; chunkY++)
                {
                    var b = chunk.GetBlock(chunkX, chunkY, chunkZ);

                    if (b.type != caveAir.type)
                        continue;

                    var under = chunk.GetBlock(chunkX, chunkY - 1, chunkZ);
                    var above = chunk.GetBlock(chunkX, chunkY + 1, chunkZ);

                    BlockValue bottomBlock;
                    // Check the floor for possible decoration
                    if (under.Block.shape.IsTerrain())
                    {
                        bottomBlock = BlockPlaceholderMap.Instance.Replace(bottomCaveDecoration, random, worldX, worldZ);

                        chunk.SetBlock(GameManager.Instance.World, chunkX, chunkY, chunkZ, bottomBlock);
                        continue;
                    }

                    // Check the ceiling to see if its a ceiling decoration
                    if (!above.Block.shape.IsTerrain())
                        continue;

                    bottomBlock = BlockPlaceholderMap.Instance.Replace(topCaveDecoration, random, worldX, worldZ);
                    chunk.SetBlock(GameManager.Instance.World, chunkX, chunkY, chunkZ, bottomBlock);
                }
            }
        }
    }

}