using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(SpawnManagerBiomes), "Update")]
public class SpawnManagerBiomes_Update
{
    private static readonly Logging.Logger logger = Logging.CreateLogger($"H_SpawnManagerBiomes_Update", LoggingLevel.INFO);

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

        if (!_spawnData.checkedPOITags)
        {
            _spawnData.checkedPOITags = true;
            FastTags<TagGroup.Poi> none = FastTags<TagGroup.Poi>.none;
            Vector3i worldPos = _spawnData.chunk.GetWorldPos();
            world.GetPOIsAtXZ(worldPos.x + 16, worldPos.x + 80 - 16, worldPos.z + 16, worldPos.z + 80 - 16, spawnManagerBiome.spawnPIs);
            for (int j = 0; j < spawnManagerBiome.spawnPIs.Count; j++)
            {
                PrefabInstance prefabInstance = spawnManagerBiome.spawnPIs[j];
                none |= prefabInstance.prefab.Tags;
            }
            _spawnData.poiTags = none;
            bool isEmpty = none.IsEmpty;
            for (int k = 0; k < biomeSpawnEntityGroupList.list.Count; k++)
            {
                BiomeSpawnEntityGroupData biomeSpawnEntityGroupData = biomeSpawnEntityGroupList.list[k];
                if ((biomeSpawnEntityGroupData.POITags.IsEmpty || biomeSpawnEntityGroupData.POITags.Test_AnySet(none)) && (isEmpty || biomeSpawnEntityGroupData.noPOITags.IsEmpty || !biomeSpawnEntityGroupData.noPOITags.Test_AnySet(none)))
                {
                    _spawnData.groupsEnabledFlags |= 1 << k;
                }
            }
        }

        int idHash = 0;
        int groupIndex = -1;
        int currentIndex = gameRandom.RandomRange(biomeSpawnEntityGroupList.list.Count);
        int maxTries = Utils.FastMin(5, biomeSpawnEntityGroupList.list.Count);
        int currentTrie = 0;

        while (currentTrie < maxTries)
        {
            BiomeSpawnEntityGroupData biomeSpawnEntityGroupData2 = biomeSpawnEntityGroupList.list[currentIndex];

            bool groupEnabled = (_spawnData.groupsEnabledFlags & (1 << currentIndex)) != 0;
            bool dayTime = biomeSpawnEntityGroupData2.daytime == EDaytime.Any || biomeSpawnEntityGroupData2.daytime == eDaytime;

            logger.Debug("");
            logger.Debug($"groupName: {biomeSpawnEntityGroupData2.entityGroupName}");
            logger.Debug($"groupEnabled: {groupEnabled}, dayTime: {dayTime}");

            if (groupEnabled && dayTime)
            {
                bool isEnemyGroup = EntityGroups.IsEnemyGroup(biomeSpawnEntityGroupData2.entityGroupName);

                if (!isEnemyGroup || _isSpawnEnemy)
                {
                    idHash = biomeSpawnEntityGroupData2.idHash;
                    ulong delayWorldTime = _spawnData.GetDelayWorldTime(idHash);

                    logger.Debug($"idHash: {idHash}");
                    logger.Debug($"worldtime: {world.worldTime}, delayWorldTime: {delayWorldTime}");

                    if (world.worldTime > delayWorldTime)
                    {
                        int num6 = biomeSpawnEntityGroupData2.maxCount;
                        if (isEnemyGroup)
                        {
                            num6 = EntitySpawner.ModifySpawnCountByGameDifficulty(num6);
                        }
                        _spawnData.ResetRespawn(idHash, world, num6);
                    }

                    // bool canSpawn = _spawnData.CanSpawn(idHash);
                    bool canSpawn = true;

                    if (_spawnData.entitesSpawned.TryGetValue(idHash, out var value))
                    {
                        logger.Debug($"count: {value.count}, maxCount: {value.maxCount}");
                        canSpawn = value.count < value.maxCount;
                    }

                    logger.Debug($"canSpawn: {canSpawn}");

                    if (canSpawn)
                    {
                        groupIndex = currentIndex;
                        break;
                    }
                }
            }

            currentTrie++;
            currentIndex = (currentIndex + 1) % biomeSpawnEntityGroupList.list.Count;
        }

        if (groupIndex < 0)
        {
            logger.Debug("groupIndex < 0");
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
