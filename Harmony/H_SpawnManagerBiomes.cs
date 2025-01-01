using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(SpawnManagerBiomes), "Update")]
public class SpawnManagerBiomes_Update
{
    private static readonly Logging.Logger logger = Logging.CreateLogger($"CaveSpawnManager");

    private static SpawnManagerBiomes spawnManagerBiome;

    private static readonly float spawnAreaSize = 40f;

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

        var world = GameManager.Instance.World;
        if (_spawnData == null || !_isSpawnEnemy || !AIDirector.CanSpawn() || world.aiDirector.BloodMoonComponent.BloodMoonActive)
        {
            return true;
        }

        var player = GetPlayerInSpawnArea(_spawnData);
        if (player is null)
        {
            return false;
        }

        var playerPosition = new Vector3i(player.GetPosition());
        if (playerPosition.y + CaveConfig.zombieSpawnMarginDeep > GameManager.Instance.World.GetTerrainHeight(playerPosition.x, playerPosition.z))
        {
            return true;
        }

        var spawnPosition = CaveSpawnManager.GetSpawnPositionNearPlayer(playerPosition, CaveConfig.minSpawnDist);
        if (spawnPosition == Vector3.zero)
        {
            logger.Debug($"no spawn position found from {playerPosition}");
            return false;
        }

        var biomeSpawnEntityGroupList = GetBiomeList(_spawnData);
        if (biomeSpawnEntityGroupList is null)
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

            if (world.worldTime > delayWorldTime || delayWorldTime == 0)
            {
                int spawnCount = EntitySpawner.ModifySpawnCountByGameDifficulty(entityGroupData.maxCount);
                ResetRespawn(_spawnData, entityGroupData, idHash, spawnCount);
            }

            if (_spawnData.CanSpawn(idHash))
            {
                groupIndex = currentIndex;
                break;
            }
        }

        if (groupIndex < 0)
        {
            return false;
        }

        Bounds bb = new Bounds(spawnPosition, new Vector3(4f, 2.5f, 4f));
        world.GetEntitiesInBounds(typeof(Entity), bb, spawnManagerBiome.spawnNearList);
        int count = spawnManagerBiome.spawnNearList.Count;
        spawnManagerBiome.spawnNearList.Clear();

        if (count > 0)
        {
            return false;
        }

        int randomFromGroup = EntityGroups.GetRandomFromGroup(biomeSpawnEntityGroupList.list[groupIndex].entityGroupName, ref spawnManagerBiome.lastClassId);
        if (randomFromGroup == 0)
        {
            _spawnData.DecMaxCount(idHash);
            return false;
        }

        _spawnData.IncCount(idHash);
        Entity entity = EntityFactory.CreateEntity(randomFromGroup, spawnPosition);

        entity.SetSpawnerSource(EnumSpawnerSource.Biome, _spawnData.chunk.Key, idHash);

        world.SpawnEntityInWorld(entity);
        world.DebugAddSpawnedEntity(entity);

        logger.Info($"spawn '{entity.GetDebugName()}' at {spawnPosition}");

        return false;
    }
    public static void ResetRespawn(ChunkAreaBiomeSpawnData _spawnData, BiomeSpawnEntityGroupData biomeSpawnEntityGroupData, int _idHash, int _maxCount)
    {
        var _world = GameManager.Instance.World;

        _spawnData.entitesSpawned.TryGetValue(_idHash, out var value);
        value.delayWorldTime = _world.worldTime + (ulong)(biomeSpawnEntityGroupData.respawnDelayInWorldTime * _world.RandomRange(0.9f, 1.1f));
        value.maxCount = _maxCount;
        _spawnData.entitesSpawned[_idHash] = value;
        _spawnData.chunk.isModified = true;
    }

    private static BiomeSpawnEntityGroupList GetBiomeList(ChunkAreaBiomeSpawnData _spawnData)
    {
        var biome = GameManager.Instance.World.Biomes.GetBiome(_spawnData.biomeId);
        if (biome == null)
        {
            logger.Warning("null biome");
            return null;
        }

        if (!BiomeSpawningClass.list.TryGetValue($"{biome.m_sBiomeName}_Cave", out var biomeSpawnEntityGroupList))
        {
            biomeSpawnEntityGroupList = BiomeSpawningClass.list["Cave"];
        }

        return biomeSpawnEntityGroupList;
    }

    private static EntityPlayer GetPlayerInSpawnArea(ChunkAreaBiomeSpawnData _spawnData)
    {
        var players = GameManager.Instance.World.GetPlayers();

        for (int i = 0; i < players.Count; i++)
        {
            EntityPlayer player = players[i];

            if (player.Spawned && player.SpawnedTicks > CaveConfig.minSpawnTicksBeforeEnemySpawn && IsPlayerInsideSpawnArea(player, _spawnData))
            {
                return player;
            }
        }

        return null;
    }

    private static bool IsPlayerInsideSpawnArea(EntityPlayer entityPlayer, ChunkAreaBiomeSpawnData _spawnData)
    {
        var bounds = new Rect(
            entityPlayer.position.x - spawnAreaSize,
            entityPlayer.position.z - spawnAreaSize,
            spawnAreaSize * 2,
            spawnAreaSize * 2
        );

        return bounds.Overlaps(_spawnData.area);
    }

}
