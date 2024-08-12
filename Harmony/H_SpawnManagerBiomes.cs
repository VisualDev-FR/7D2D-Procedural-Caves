using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using WorldGenerationEngineFinal;


[HarmonyPatch(typeof(SpawnManagerBiomes), "Update")]
public class SpawnManagerBiomes_Update
{
    public static bool Prefix(SpawnManagerBiomes __instance, string _spawnerName, bool _bSpawnEnemyEntities, object _userData, ref List<Entity> ___spawnNearList, ref int ___lastClassId)
    {
        if (!GameUtils.IsPlaytesting())
        {
            return SpawnUpdate(_spawnerName, _bSpawnEnemyEntities, _userData as ChunkAreaBiomeSpawnData, ref ___spawnNearList, ref ___lastClassId);
        }

        return true;
    }

    public static bool SpawnUpdate(string _spawnerName, bool _bSpawnEnemyEntities, ChunkAreaBiomeSpawnData _chunkBiomeSpawnData, ref List<Entity> spawnNearList, ref int lastClassId)
    {
        var deepCaveThreshold = 30;

        if (_chunkBiomeSpawnData == null)
        {
            return true;
        }
        if (_bSpawnEnemyEntities)
        {
            if (GameStats.GetInt(EnumGameStats.EnemyCount) >= GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedZombies))
            {
                _bSpawnEnemyEntities = false;
            }
            else if (GameManager.Instance.World.aiDirector.BloodMoonComponent.BloodMoonActive)
            {
                _bSpawnEnemyEntities = false;
            }
        }

        if (!_bSpawnEnemyEntities && GameStats.GetInt(EnumGameStats.AnimalCount) >= GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedAnimals))
        {
            return true;
        }
        var rectOverlaps = false;
        var players = GameManager.Instance.World.GetPlayers();

        // Player Position.
        var playerPos = Vector3.zero;
        foreach (var player in players)
        {
            if (!player.Spawned) continue;

            playerPos = player.GetPosition();
            var rect = new Rect(playerPos.x - 40f, playerPos.z - 40f, 80f, 20f);
            if (rect.Overlaps(_chunkBiomeSpawnData.area))
            {
                rectOverlaps = true;
                break;
            }
        }

        // No valid player position.
        if (playerPos == Vector3.zero)
            return true;

        // Don't allow above ground spawning.
        var playerPosition = new Vector3i(playerPos);
        float terrainHeight = GameManager.Instance.World.GetTerrainHeight(playerPosition.x, playerPosition.z);

        if (playerPosition.y > terrainHeight || !rectOverlaps)
            return true;

        var spawnPosition = GetZombieSpawnPosition(playerPosition);

        if (spawnPosition == Vector3.zero)
            return false;

        var biome = GameManager.Instance.World.Biomes.GetBiome(_chunkBiomeSpawnData.biomeId);
        if (biome == null)
        {
            return false;
        }

        // Customize which spawning.xml entry to we want to use for spawns.
        var caveType = spawnPosition.y < deepCaveThreshold ? "DeepCave" : "Cave";

        // Search for the biome_Cave spawn group. If not found, load the generic Cave one.
        var biomeSpawnEntityGroupList = BiomeSpawningClass.list[biome.m_sBiomeName + "_" + caveType];
        if (biomeSpawnEntityGroupList == null)
        {
            biomeSpawnEntityGroupList = BiomeSpawningClass.list[caveType];
        }

        if (biomeSpawnEntityGroupList == null)
            return false;

        var edaytime = GameManager.Instance.World.IsDaytime() ? EDaytime.Day : EDaytime.Night;
        var gameRandom = GameManager.Instance.World.GetGameRandom();

        string entityGroupName = null;
        var index = -1;
        var randomIndex = gameRandom.RandomRange(biomeSpawnEntityGroupList.list.Count);

        for (int j = 0; j < 5; j++)
        {
            BiomeSpawnEntityGroupData biomeSpawnEntityGroupData2 = biomeSpawnEntityGroupList.list[randomIndex];
            if (biomeSpawnEntityGroupData2.daytime != EDaytime.Any && biomeSpawnEntityGroupData2.daytime != edaytime)
            {
                bool isEnemyGroup = EntityGroups.IsEnemyGroup(biomeSpawnEntityGroupData2.entityGroupRefName);
                if (!isEnemyGroup || _bSpawnEnemyEntities)
                {
                    int spawnCount = biomeSpawnEntityGroupData2.maxCount;
                    if (isEnemyGroup)
                    {
                        spawnCount = EntitySpawner.ModifySpawnCountByGameDifficulty(spawnCount);
                    }

                    entityGroupName = biomeSpawnEntityGroupData2.entityGroupRefName + "_" + biomeSpawnEntityGroupData2.daytime.ToStringCached<EDaytime>();
                    ulong respawnDelayWorldTime = _chunkBiomeSpawnData.GetRespawnDelayWorldTime(entityGroupName);
                    if (respawnDelayWorldTime > 0UL)
                    {
                        if (GameManager.Instance.World.worldTime < respawnDelayWorldTime)
                        {
                            break;
                        }
                        _chunkBiomeSpawnData.ClearRespawn(entityGroupName);
                    }
                    if (_chunkBiomeSpawnData.GetEntitiesSpawned(entityGroupName) < spawnCount)
                    {

                        index = randomIndex;
                        break;
                    }
                }
            }
            randomIndex = (randomIndex + 1) % biomeSpawnEntityGroupList.list.Count;
        }

        if (index < 0)
            return false;

        var bb = new Bounds(spawnPosition, new Vector3(4f, 2.5f, 4f));
        GameManager.Instance.World.GetEntitiesInBounds(typeof(Entity), bb, spawnNearList);
        var count = spawnNearList.Count;
        spawnNearList.Clear();

        if (count > 0)
            return false;

        var biomeSpawnEntityGroupData3 = biomeSpawnEntityGroupList.list[index];
        var entityID = EntityGroups.GetRandomFromGroup(biomeSpawnEntityGroupData3.entityGroupRefName, ref lastClassId);
        _chunkBiomeSpawnData.IncEntitiesSpawned(entityGroupName);

        Log.Out($"[Caves] Spawning: {entityID} at {spawnPosition}, playerPos={playerPosition}");

        SpawnEntity(entityID, spawnPosition, _chunkBiomeSpawnData, entityGroupName);

        return false;
    }

    public static Vector3 GetZombieSpawnPosition(Vector3 playerPosition)
    {
        GameRandom random = GameRandomManager.instance.CreateGameRandom(playerPosition.GetHashCode());
        List<CaveBlock> spawnPositions = CaveGenerator.caveBlocksProvider.GetSpawnPositions(playerPosition);

        if (spawnPositions.Count == 0)
            return Vector3.zero;

        CaveBlock caveblock = spawnPositions[random.Next(spawnPositions.Count)];

        Vector3 spawnPosition = caveblock.ToWorldPos();

        return spawnPosition;
    }

    private static void SpawnEntity(int id, Vector3 spawnPosition, ChunkAreaBiomeSpawnData _chunkBiomeSpawnData, string entityGroupName)
    {
        var entity = EntityFactory.CreateEntity(id, spawnPosition);

        if (entity == null)
        {
            Log.Error($"[Cave] null entity for id '{id}'");
            return;
        }
        entity.SetSpawnerSource(EnumSpawnerSource.Dynamic, _chunkBiomeSpawnData.chunk.Key, entityGroupName);

        var myEntity = entity as EntityAlive;

        if (myEntity)
            myEntity.SetSleeper();

        GameManager.Instance.World.SpawnEntityInWorld(entity);
        GameManager.Instance.World.DebugAddSpawnedEntity(entity);
    }
}
