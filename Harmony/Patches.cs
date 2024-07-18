using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Xml.Linq;
using WorldGenerationEngineFinal;


public static class ProceduralCaveSystem
{
    [HarmonyPatch(typeof(SpawnManagerBiomes), "Update")]
    public class CaveProjectSpawnmanagerBiomes
    {
        // We want to run our cave spawning class right under the main biome spawner.
        public static bool Prefix(SpawnManagerBiomes __instance, string _spawnerName, bool _bSpawnEnemyEntities, object _userData, ref List<Entity> ___spawnNearList, ref int ___lastClassId)
        {
            // if (!GameUtils.IsPlaytesting())
            // {
            //     SpawnUpdate(_spawnerName, _bSpawnEnemyEntities, _userData as ChunkAreaBiomeSpawnData,
            //         ref ___spawnNearList, ref ___lastClassId);
            // }

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

            var spawnPosition = new Vector3(x, y, z);

            // Mob is above terrain; ignore.
            if (spawnPosition.y > terrainHeight)
                return;

            var biome = GameManager.Instance.World.Biomes.GetBiome(_chunkBiomeSpawnData.biomeId);
            if (biome == null)
            {
                return;
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

            var bb = new Bounds(spawnPosition, new Vector3(4f, 2.5f, 4f));
            GameManager.Instance.World.GetEntitiesInBounds(typeof(Entity), bb, spawnNearList);
            var count = spawnNearList.Count;
            spawnNearList.Clear();

            if (count > 0)
                return;

            var biomeSpawnEntityGroupData3 = biomeSpawnEntityGroupList.list[index];
            var randomFromGroup = EntityGroups.GetRandomFromGroup(biomeSpawnEntityGroupData3.entityGroupRefName, ref lastClassId);
            _chunkBiomeSpawnData.IncEntitiesSpawned(entityGroupName);

            SpawnEntity(randomFromGroup, spawnPosition, _chunkBiomeSpawnData, entityGroupName);
        }

        private static void SpawnEntity(int id, Vector3 spawnPosition, ChunkAreaBiomeSpawnData _chunkBiomeSpawnData, string entityGroupName)
        {
            var entity = EntityFactory.CreateEntity(id, spawnPosition);
            entity.SetSpawnerSource(EnumSpawnerSource.Dynamic, _chunkBiomeSpawnData.chunk.Key, entityGroupName);

            var myEntity = entity as EntityAlive;
            if (myEntity)
                myEntity.SetSleeper();

            Log.Out($"[Caves] Spawning: {myEntity.entityId} at {spawnPosition}");
            GameManager.Instance.World.SpawnEntityInWorld(entity);
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
            CaveGenerator.GenerateCave(_chunk);
        }
    }


    [HarmonyPatch(typeof(DynamicPrefabDecorator), "DecorateChunk")]
    [HarmonyPatch(new[] { typeof(World), typeof(Chunk), typeof(bool) })]
    public class CaveProjectDynamicPrefabDecorator
    {
        public static void Postfix(DynamicPrefabDecorator __instance, Chunk _chunk)
        {
            // LegacyCaveSystem.AddDecorationsToCave(_chunk);
        }
    }


    [HarmonyPatch(typeof(DynamicPrefabDecorator), "Load")]
    public class DynamicPrefabDecorator_Load
    {
        private static XmlFile ReadCavePrefabsDatas(string _path)
        {

            if (!SdFile.Exists(_path + "/cavePrefabs.xml"))
            {
                Log.Out($"[Caves] cavePrefabs.xml not found in '{_path}'");
                return null;
            }
            XmlFile xmlFile;
            try
            {
                xmlFile = new XmlFile(_path, "cavePrefabs.xml");
            }
            catch (Exception ex)
            {
                Log.Error("[Caves] Loading cavePrefabs.xml file for level '" + System.IO.Path.GetFileName(_path) + "': " + ex.Message);
                Log.Exception(ex);
                return null;
            }

            return xmlFile;
        }

        private static void LoadCavePrefab(DynamicPrefabDecorator __instance, XElement prefabEntry)
        {
            if (!prefabEntry.HasAttribute("name"))
                return;

            Vector3i prefabPosition = Vector3i.Parse(prefabEntry.GetAttribute("position"));
            string prefabName = prefabEntry.GetAttribute("name");
            byte prefabRotation = 0;

            if (prefabEntry.HasAttribute("rotation"))
                prefabRotation = byte.Parse(prefabEntry.GetAttribute("rotation"));

            Prefab prefabRotated = __instance.GetPrefabRotated(prefabName, prefabRotation);

            if (prefabRotated == null)
            {
                Log.Warning("Could not load prefab '" + prefabName + "'. Skipping it");
                return;
            }

            if (prefabRotated.bTraderArea)
                __instance.AddTrader(new TraderArea(prefabPosition, prefabRotated.size, prefabRotated.TraderAreaProtect, prefabRotated.TeleportVolumes));

            PrefabInstance prefabInstance = new PrefabInstance(__instance.id++, prefabRotated.location, prefabPosition, prefabRotation, prefabRotated, 0);
            __instance.AddPrefab(prefabInstance, prefabInstance.prefab.HasQuestTag());
        }

        public static void Postfix(DynamicPrefabDecorator __instance, string _path)
        {
            XmlFile xmlFile = ReadCavePrefabsDatas(_path);

            if (xmlFile == null)
                return;

            int i = 0;

            foreach (XElement prefabEntry in xmlFile.XmlDoc.Root.Elements("decoration"))
            {
                try
                {
                    i++;
                    LoadCavePrefab(__instance, prefabEntry);
                }
                catch (Exception ex2)
                {
                    Log.Error("Loading prefabs xml file for level '" + System.IO.Path.GetFileName(_path) + "': " + ex2.Message);
                    Log.Exception(ex2);
                }
            }
            __instance.SortPrefabs();

            Log.Out($"[Caves] success loading of cavePrefabs");
            return;
        }
    }


    [HarmonyPatch(typeof(PrefabManager), "LoadPrefabs")]
    public static class PrefabManager_LoadPrefabs
    {
        public static IEnumerator LoadPrefabs()
        {
            PrefabManager.ClearDisplayed();
            if (PrefabManager.AllPrefabDatas.Count != 0)
            {
                yield break;
            }
            MicroStopwatch ms = new MicroStopwatch(_bStart: true);
            List<PathAbstractions.AbstractedLocation> prefabs = PathAbstractions.PrefabsSearchPaths.GetAvailablePathsList(null, null, null, _ignoreDuplicateNames: true);
            FastTags<TagGroup.Poi> filter = FastTags<TagGroup.Poi>.Parse("navonly,devonly,testonly,biomeonly");

            for (int i = 0; i < prefabs.Count; i++)
            {
                PathAbstractions.AbstractedLocation location = prefabs[i];

                int prefabCount = location.Folder.LastIndexOf("/Prefabs/");
                if (prefabCount >= 0 && location.Folder.Substring(prefabCount + 8, 5).EqualsCaseInsensitive("/test"))
                    continue;

                PrefabData prefabData = PrefabData.LoadPrefabData(location);

                if (prefabData == null || prefabData.Tags.IsEmpty)
                    Log.Warning("Could not load prefab data for " + location.Name);

                if (prefabData.Tags.Test_AnySet(CavePlanner.caveTags))
                {
                    CavePlanner.AllCavePrefabs[location.Name.ToLower()] = prefabData;
                }
                else if (!prefabData.Tags.Test_AnySet(filter))
                {
                    PrefabManager.AllPrefabDatas[location.Name.ToLower()] = prefabData;
                }

                if (ms.ElapsedMilliseconds > 500)
                {
                    yield return null;
                    ms.ResetAndRestart();
                }
            }

            if (CavePlanner.AllCavePrefabs.Count == 0)
                Log.Error($"[Cave] No cave prefab was loaded.");

            foreach (var prefab in CavePlanner.AllCavePrefabs.Values)
            {
                Log.Out($"[Cave] cave prefab found: {prefab.Name}");
            }

            Log.Out($"LoadPrefabs {PrefabManager.AllPrefabDatas.Count} of {prefabs.Count} in {ms.ElapsedMilliseconds * 0.001f}");
        }

        public static bool Prefix(ref IEnumerator __result)
        {
            __result = LoadPrefabs();

            return false;
        }
    }


    [HarmonyPatch(typeof(PrefabManager), "GetWildernessPrefab")]
    public static class PrefabManager_GetWildernessPrefab
    {
        public static bool Prefix(FastTags<TagGroup.Poi> _withoutTags, FastTags<TagGroup.Poi> _markerTags, Vector2i minSize, Vector2i maxSize, Vector2i center, bool _isRetry, ref PrefabData __result)
        {
            int entrancesAdded = CavePlanner.GetUsedCavePrefabs().Count;

            if (entrancesAdded >= 20)
                return true;

            Log.Out($"[Cave] addedPrefabs = {entrancesAdded}");

            var prefabs = CavePlanner.GetCaveEntrancePrefabs();

            __result = prefabs[CavePlanner.rand.Next(prefabs.Count)];

            return false;
        }
    }


    [HarmonyPatch(typeof(WorldBuilder), "GenerateFromUI")]
    public static class WorldBuilder_GenerateFromUI
    {
        public static WorldBuilder worldBuilder;

        public static IEnumerator GenerateFromUI()
        {
            worldBuilder.IsCanceled = false;
            worldBuilder.IsFinished = false;
            worldBuilder.totalMS = new MicroStopwatch(_bStart: true);
            yield return worldBuilder.SetMessage("Starting");
            yield return new WaitForSeconds(0.1f);
            yield return worldBuilder.GenerateData();
        }

        public static IEnumerator GenerateFromUIPostFix()
        {
            CavePlanner.Cleanup();

            yield return GenerateFromUI();

            var addedCaveEntrances = CavePlanner.GetUsedCavePrefabs();

            Log.Out($"[Cave] {addedCaveEntrances.Count} Cave entrance added.");

            foreach (var prefab in addedCaveEntrances)
            {
                Log.Out($"[Cave] Cave entrance added at {prefab.boundingBoxPosition}");
            }

            yield return CavePlanner.GenerateCaveMap();
        }

        public static bool Prefix(WorldBuilder __instance, ref IEnumerator __result)
        {
            worldBuilder = __instance;
            __result = GenerateFromUIPostFix();
            return false;
        }
    }

    [HarmonyPatch(typeof(WorldBuilder), "saveRawHeightmap")]
    public static class WorldBuilder_saveRawHeightmap
    {
        public static bool Prefix()
        {
            Log.Out("start WorldBuilder_saveRawHeightmap postfix");
            CavePlanner.SaveCaveMap();

            return true;
        }
    }


    [HarmonyPatch(typeof(GameManager), "createWorld")]
    public static class GameManager_createWorld
    {
        public static bool Prefix(string _sWorldName, string _sGameName, List<WallVolume> _wallVolumes, bool _fixedSizeCC = false)
        {
            CaveGenerator.LoadCaveMap(_sWorldName);
            return true;
        }
    }

}
