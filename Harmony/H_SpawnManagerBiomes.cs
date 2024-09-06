using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(SpawnManagerBiomes), "Update")]
public class SpawnManagerBiomes_Update
{
    private static SpawnManagerBiomes spawnManagerBiome;

    private static readonly int minEnemySpawnDist = 40;

    public static bool Prefix(SpawnManagerBiomes __instance, string _spawnerName, bool _bSpawnEnemyEntities, object _userData)
    {
        if (!GameUtils.IsPlaytesting() && CaveGenerator.isEnabled)
        {
            spawnManagerBiome = __instance;
            return SpawnUpdate(_spawnerName, _bSpawnEnemyEntities, _userData as ChunkAreaBiomeSpawnData);
        }

        return true;
    }

    public static bool SpawnUpdate(string _spawnerName, bool _isSpawnEnemy, ChunkAreaBiomeSpawnData _chunkBiomeSpawnData)
    {
        var world = GameManager.Instance.World;
        var deepCaveThreshold = 30;

        if (_chunkBiomeSpawnData == null)
            return false;

        _isSpawnEnemy &= !AIDirector.CanSpawn() || world.aiDirector.BloodMoonComponent.BloodMoonActive;

        if (!_isSpawnEnemy && GameStats.GetInt(EnumGameStats.AnimalCount) >= GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedAnimals))
            return false;

        var players = world.GetPlayers();
        var playerPosition = Vector3i.zero;

        for (int i = 0; i < players.Count; i++)
        {
            EntityPlayer entityPlayer = players[i];

            if (entityPlayer.Spawned && new Rect(entityPlayer.position.x - 40f, entityPlayer.position.z - 40f, 80f, 80f).Overlaps(_chunkBiomeSpawnData.area))
            {
                playerPosition = new Vector3i(entityPlayer.GetPosition());
                break;
            }
        }

        if (playerPosition == Vector3i.zero)
            return false;

        if (playerPosition.y > GameManager.Instance.World.GetTerrainHeight(playerPosition.x, playerPosition.z))
            return true;

        var spawnPosition = GetZombieSpawnPosition(playerPosition);
        if (spawnPosition == Vector3.zero)
        {
            return false;
        }

        var biome = GameManager.Instance.World.Biomes.GetBiome(_chunkBiomeSpawnData.biomeId);
        if (biome == null)
        {
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

        /* if (!_chunkBiomeSpawnData.checkedPOITags)
        {
            _chunkBiomeSpawnData.checkedPOITags = true;
            FastTags<TagGroup.Poi> none = FastTags<TagGroup.Poi>.none;
            Vector3i worldPos = _chunkBiomeSpawnData.chunk.GetWorldPos();
            world.GetPOIsAtXZ(worldPos.x + 16, worldPos.x + 80 - 16, worldPos.z + 16, worldPos.z + 80 - 16, spawnPIs);
            for (int j = 0; j < spawnPIs.Count; j++)
            {
                PrefabInstance prefabInstance = spawnPIs[j];
                none |= prefabInstance.prefab.Tags;
            }
            _chunkBiomeSpawnData.poiTags = none;
            bool isEmpty = none.IsEmpty;
            for (int k = 0; k < biomeSpawnEntityGroupList.list.Count; k++)
            {
                BiomeSpawnEntityGroupData biomeSpawnEntityGroupData = biomeSpawnEntityGroupList.list[k];
                if ((biomeSpawnEntityGroupData.POITags.IsEmpty || biomeSpawnEntityGroupData.POITags.Test_AnySet(none)) && (isEmpty || biomeSpawnEntityGroupData.noPOITags.IsEmpty || !biomeSpawnEntityGroupData.noPOITags.Test_AnySet(none)))
                {
                    _chunkBiomeSpawnData.groupsEnabledFlags |= 1 << k;
                }
            }
        } */

        string entityGroupName = null;
        int randomIndex = gameRandom.RandomRange(biomeSpawnEntityGroupList.list.Count);
        int maxTries = Utils.FastMin(5, biomeSpawnEntityGroupList.list.Count);
        int index = -1;

        for (int i = 0; i < maxTries; i++, randomIndex = (randomIndex + 1) % biomeSpawnEntityGroupList.list.Count)
        {
            BiomeSpawnEntityGroupData spawnGroup = biomeSpawnEntityGroupList.list[randomIndex];

            if (spawnGroup.daytime != EDaytime.Any && spawnGroup.daytime != eDaytime)
            {
                continue;
            }

            bool isEnemyGroup = EntityGroups.IsEnemyGroup(spawnGroup.entityGroupRefName);
            int spawnCount = spawnGroup.maxCount;

            if (isEnemyGroup)
                spawnCount = EntitySpawner.ModifySpawnCountByGameDifficulty(spawnCount);

            entityGroupName = spawnGroup.entityGroupRefName + "_" + spawnGroup.daytime.ToStringCached<EDaytime>();
            ulong respawnDelayWorldTime = _chunkBiomeSpawnData.GetRespawnDelayWorldTime(entityGroupName);

            if (respawnDelayWorldTime != 0)
            {
                if (world.worldTime < respawnDelayWorldTime)
                {
                    continue;
                }
                _chunkBiomeSpawnData.ClearRespawn(entityGroupName);
            }

            if (_chunkBiomeSpawnData.GetEntitiesSpawned(entityGroupName) < spawnCount)
            {
                index = randomIndex;
                break;
            }
        }

        if (index < 0)
        {
            return false;
        }

        var bb = new Bounds(spawnPosition, new Vector3(4f, 2.5f, 4f));
        var spawnNearList = new List<Entity>();

        world.GetEntitiesInBounds(typeof(Entity), bb, spawnNearList);

        if (spawnNearList.Count > 0)
        {
            return false;
        }

        var spawnedGroup = biomeSpawnEntityGroupList.list[index];
        var entityID = EntityGroups.GetRandomFromGroup(spawnedGroup.entityGroupRefName, ref spawnManagerBiome.lastClassId);
        if (entityID == 0)
        {
            _chunkBiomeSpawnData.SetRespawnDelay(entityGroupName, world.worldTime, world.Biomes);
            return false;
        }

        _chunkBiomeSpawnData.IncEntitiesSpawned(entityGroupName);

        Entity entity = EntityFactory.CreateEntity(entityID, spawnPosition);
        entity.SetSpawnerSource(EnumSpawnerSource.Biome, _chunkBiomeSpawnData.chunk.Key, entityGroupName);
        world.SpawnEntityInWorld(entity);

        float spawnDeadChance = spawnedGroup.spawnDeadChance;
        if (spawnDeadChance > 0f && gameRandom.RandomFloat < spawnDeadChance)
            entity.Kill(DamageResponse.New(_fatal: true));

        Log.Out($"[Caves] Spawning entity: {entityID} at {spawnPosition}, playerPos=[{playerPosition}]");

        return false;
    }

    public static Vector3 GetZombieSpawnPosition(Vector3i playerPosition)
    {
        // TODO: see world.GetRandomSpawnPositionInAreaMinMaxToPlayers

        GameRandom random = GameRandomManager.instance.CreateGameRandom(playerPosition.GetHashCode());
        List<CaveBlock> spawnPositions = CaveGenerator.caveBlocksProvider.GetSpawnPositionsFromPlayer(playerPosition, minEnemySpawnDist);

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
