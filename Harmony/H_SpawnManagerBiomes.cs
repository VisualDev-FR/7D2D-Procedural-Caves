using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(SpawnManagerBiomes), "Update")]
public class SpawnManagerBiomes_Update
{
    private static SpawnManagerBiomes spawnManagerBiome;

    public static bool Prefix(SpawnManagerBiomes __instance, string _spawnerName, bool _bSpawnEnemyEntities, object _userData)
    {
        if (!GameManager.Instance.IsEditMode() && !GameUtils.IsPlaytesting() && CaveGenerator.isEnabled)
        {
            spawnManagerBiome = __instance;
            return SpawnUpdate(_spawnerName, _bSpawnEnemyEntities, _userData as ChunkAreaBiomeSpawnData);
        }

        return true;
    }

    public static bool SpawnUpdate(string _spawnerName, bool _isSpawnEnemy, ChunkAreaBiomeSpawnData _spawnData)
    {
        if (!CaveConfig.enableCaveSpawn)
        {
            return false;
        }

        var logger = Logging.CreateLogger($"CaveSpawnManager");

        var world = GameManager.Instance.World;
        var deepCaveThreshold = 30;

        if (_spawnData == null || !_isSpawnEnemy || !AIDirector.CanSpawn() || world.aiDirector.BloodMoonComponent.BloodMoonActive)
        {
            return true;
        }

        var players = world.GetPlayers();
        var playerPosition = Vector3i.zero;

        for (int i = 0; i < players.Count; i++)
        {
            EntityPlayer entityPlayer = players[i];

            if (entityPlayer.Spawned && new Rect(entityPlayer.position.x - 40f, entityPlayer.position.z - 40f, 80f, 80f).Overlaps(_spawnData.area))
            {
                playerPosition = new Vector3i(entityPlayer.GetPosition());
                break;
            }
        }

        if (playerPosition == Vector3i.zero)
            return false;

        if (playerPosition.y + CaveConfig.zombieSpawnMarginDeep > GameManager.Instance.World.GetTerrainHeight(playerPosition.x, playerPosition.z))
            return true;

        var biome = GameManager.Instance.World.Biomes.GetBiome(_spawnData.biomeId);
        if (biome == null)
        {
            logger.Warning("null biome");
            return false;
        }

        var spawnPosition = CaveSpawnManager.GetSpawnPositionNearPlayer(playerPosition, CaveConfig.minSpawnDist);

        if (spawnPosition == Vector3.zero)
        {
            Logging.Debug($"no spawn position found from {playerPosition}");
            return false;
        }

        var caveType = spawnPosition.y < deepCaveThreshold ? "DeepCave" : "Cave";
        var biomeSpawnEntityGroupList = BiomeSpawningClass.list[biome.m_sBiomeName + "_" + caveType] ?? BiomeSpawningClass.list[caveType];

        if (biomeSpawnEntityGroupList == null)
        {
            return false;
        }

        var eDaytime = world.IsDaytime() ? EDaytime.Day : EDaytime.Night;
        var gameRandom = world.GetGameRandom();

        int idHash = 0;
        int groupIndex = -1;
        int currentIndex = gameRandom.RandomRange(biomeSpawnEntityGroupList.list.Count);
        int maxTries = Utils.FastMin(5, biomeSpawnEntityGroupList.list.Count);

        for (int i = 0; i < maxTries; i++)
        {
            currentIndex = (currentIndex + 1) % biomeSpawnEntityGroupList.list.Count;

            var entityGroupData = biomeSpawnEntityGroupList.list[currentIndex];

            if (entityGroupData.daytime != EDaytime.Any && entityGroupData.daytime != eDaytime)
            {
                continue;
            }

            idHash = entityGroupData.idHash;
            ulong delayWorldTime = _spawnData.GetDelayWorldTime(idHash);

            if (world.worldTime > delayWorldTime)
            {
                int spawnCount = EntitySpawner.ModifySpawnCountByGameDifficulty(entityGroupData.maxCount);
                _spawnData.ResetRespawn(idHash, world, spawnCount);
            }

            groupIndex = currentIndex;
            break;

        }

        if (groupIndex < 0)
        {
            return false;
        }

        Bounds bb = new Bounds(spawnPosition, new Vector3(4f, 2.5f, 4f));
        world.GetEntitiesInBounds(typeof(Entity), bb, spawnManagerBiome.spawnNearList);
        int count = spawnManagerBiome.spawnNearList.Count;
        spawnManagerBiome.spawnNearList.Clear();

        if (count <= 0)
        {
            int randomFromGroup = EntityGroups.GetRandomFromGroup(biomeSpawnEntityGroupList.list[groupIndex].entityGroupName, ref spawnManagerBiome.lastClassId);
            if (randomFromGroup != 0)
            {
                _spawnData.IncCount(idHash);
                Entity entity = EntityFactory.CreateEntity(randomFromGroup, spawnPosition);
                entity.SetSpawnerSource(EnumSpawnerSource.Biome, _spawnData.chunk.Key, idHash);
                world.SpawnEntityInWorld(entity);
                world.DebugAddSpawnedEntity(entity);

                logger.Info($"spawn '{entity.GetDebugName()}' at {spawnPosition}");
            }
        }

        return false;
    }
}
