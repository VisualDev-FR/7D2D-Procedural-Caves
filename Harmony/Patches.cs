using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;


namespace Harmony
{
    public class SCoreCaveProject
    {
        // private static readonly float depth = 30;

        // // Make the world darker underground
        // [HarmonyPatch(typeof(SkyManager))]
        // [HarmonyPatch("Update")]
        // public class CaveProjectSkyManager
        // {
        //     public static bool Prefix(float ___sunIntensity, float ___sMaxSunIntensity)
        //     {
        //         if (GamePrefs.GetString(EnumGamePrefs.GameWorld) == "Empty" || GamePrefs.GetString(EnumGamePrefs.GameWorld) == "Playtesting")
        //             return true;


        //         if (GameManager.Instance.World.GetPrimaryPlayer() == null)
        //             return true;

        //         if (GameManager.Instance.World.GetPrimaryPlayer().position.y < depth) SkyManager.SetSunIntensity(0.1f);
        //         return true;
        //     }

        //     public static void Postfix(float ___sunIntensity, float ___sMaxSunIntensity)
        //     {
        //         if (GameManager.Instance.World.GetPrimaryPlayer() == null)
        //             return;

        //         if (GameManager.Instance.World.GetPrimaryPlayer().position.y < depth) SkyManager.SetSunIntensity(0.1f);
        //     }
        // }

        [HarmonyPatch(typeof(SpawnManagerBiomes))]
        [HarmonyPatch("Update")]
        public class CaveProjectSpawnmanagerBiomes
        {
            // We want to run our cave spawning class right under the main biome spawner.
            public static bool Prefix(SpawnManagerBiomes __instance, string _spawnerName, bool _bSpawnEnemyEntities, object _userData, ref List<Entity> ___spawnNearList, ref int ___lastClassId)
            {
                if (!GameUtils.IsPlaytesting())
                {
                    SpawnUpdate(_spawnerName, _bSpawnEnemyEntities, _userData as ChunkAreaBiomeSpawnData,
                        ref ___spawnNearList, ref ___lastClassId);
                }

                return true;
            }
            // This method is a modified version of vanilla, doing the same checks and balances.
            // However, we do use the player position a bit more, and we change which biome spawning group we
            // will use, when below the terrain.
            public static void SpawnUpdate(string _spawnerName, bool _bSpawnEnemyEntities, ChunkAreaBiomeSpawnData _chunkBiomeSpawnData, ref List<Entity> spawnNearList, ref int lastClassId)
            {
                var deepCaveThreshold = 30;

                if (_chunkBiomeSpawnData == null)
                {
                    return;
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
                    return;
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
                    return;

                // Don't allow above ground spawning.
                var playerPosition = new Vector3i(playerPos);
                float terrainHeight = GameManager.Instance.World.GetTerrainHeight(playerPosition.x, playerPosition.z);
                if (playerPosition.y > terrainHeight)
                    return;

                var maxDistance = new Vector3(60, 20, 60);
                if (!rectOverlaps || !GameManager.Instance.World.FindRandomSpawnPointNearPositionUnderground(playerPosition, 16, out int x, out int y, out int z, maxDistance))
                {
                    return;
                }

                var vector = new Vector3(x, y, z);

                // Mob is above terrain; ignore.
                if (vector.y > terrainHeight)
                    return;

                var biome = GameManager.Instance.World.Biomes.GetBiome(_chunkBiomeSpawnData.biomeId);
                if (biome == null)
                {
                    return;
                }

                // Customize which spawning.xml entry to we want to use for spawns.
                var caveType = vector.y < deepCaveThreshold ? "DeepCave" : "Cave";

                // Search for the biome_Cave spawn group. If not found, load the generic Cave one.
                var biomeSpawnEntityGroupList = BiomeSpawningClass.list[biome.m_sBiomeName + "_" + caveType];
                if (biomeSpawnEntityGroupList == null)
                {
                    biomeSpawnEntityGroupList = BiomeSpawningClass.list[caveType];
                }

                if (biomeSpawnEntityGroupList == null)
                    return;

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
                    return;

                var bb = new Bounds(vector, new Vector3(4f, 2.5f, 4f));
                GameManager.Instance.World.GetEntitiesInBounds(typeof(Entity), bb, spawnNearList);
                var count = spawnNearList.Count;
                spawnNearList.Clear();

                if (count > 0)
                    return;

                var biomeSpawnEntityGroupData3 = biomeSpawnEntityGroupList.list[index];
                var randomFromGroup = EntityGroups.GetRandomFromGroup(biomeSpawnEntityGroupData3.entityGroupRefName, ref lastClassId);
                var spawnDeadChance = biomeSpawnEntityGroupData3.spawnDeadChance;
                _chunkBiomeSpawnData.IncEntitiesSpawned(entityGroupName);
                var entity = EntityFactory.CreateEntity(randomFromGroup, vector);
                entity.SetSpawnerSource(EnumSpawnerSource.Dynamic, _chunkBiomeSpawnData.chunk.Key, entityGroupName);
                var myEntity = entity as EntityAlive;

                if (myEntity)
                    myEntity.SetSleeper();

                Log.Out($"[Caves] Spawning: {myEntity.entityId} at {vector}");
                GameManager.Instance.World.SpawnEntityInWorld(entity);

                if (spawnDeadChance > 0f && gameRandom.RandomFloat < spawnDeadChance) entity.Kill(DamageResponse.New(true));

                GameManager.Instance.World.DebugAddSpawnedEntity(entity);
            }
        }

        [HarmonyPatch(typeof(TerrainGeneratorWithBiomeResource))]
        [HarmonyPatch("GenerateTerrain")]
        [HarmonyPatch(new[] { typeof(World), typeof(Chunk), typeof(GameRandom), typeof(Vector3i), typeof(Vector3i), typeof(bool), typeof(bool) })]
        public class CaveProjectTerrainGeneratorWithBiomeResource
        {
            public static void Postfix(Chunk _chunk)
            {
                LegacyCaveSystem.AddCaveToChunk(_chunk);
                // LegacyCaveSystem.Add3DCaveToChunk(_chunk);
            }
        }
    }
}