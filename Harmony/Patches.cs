using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Xml.Linq;
using WorldGenerationEngineFinal;
using System.Linq;

using Random = System.Random;
using System.ComponentModel;


public class ProceduralCaveSystem
{
    public static Dictionary<string, PrefabData> AllCavePrefabs = new Dictionary<string, PrefabData>();

    public static FastTags<TagGroup.Poi> CaveEntranceTags = FastTags<TagGroup.Poi>.Parse("entrance");

    public static FastTags<TagGroup.Poi> caveTags = FastTags<TagGroup.Poi>.Parse("cave");

    public static Random rand = CaveBuilder.rand;


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
            // LegacyCaveSystem.Add2DCaveToChunk(_chunk);
            // LegacyCaveSystem.Add3DCaveToChunk(_chunk);
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

                if (prefabData.Tags.Test_AnySet(caveTags))
                {
                    AllCavePrefabs[location.Name.ToLower()] = prefabData;
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

            if (AllCavePrefabs.Count == 0)
                Log.Error($"[Cave] No cave prefab was loaded.");

            foreach (var prefab in AllCavePrefabs.Values)
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


    [HarmonyPatch(typeof(WorldBuilder), "GenerateData")]
    public static class WorldBuilder_GenerateData
    {
        public static WorldBuilder worldBuilder;

        public static List<PrefabData> GetCaveEntrancePrefabs()
        {
            var prefabDatas = new List<PrefabData>();

            foreach (var prefabData in AllCavePrefabs.Values)
            {
                if (prefabData.Tags.Test_AnySet(CaveEntranceTags))
                {
                    prefabDatas.Add(prefabData);
                }
            }

            return prefabDatas;
        }

        private static bool IsValidPosition(int x, int y, BiomeType biome, int centerHeight)
        {
            var worldSize = worldBuilder.WorldSize;

            return x < worldSize && x >= 0 && y < worldSize && y >= 0
                && worldBuilder.GetWater(x, y) <= 0
                && biome == worldBuilder.GetBiome(x, y)
                && Math.Abs(Mathf.CeilToInt(worldBuilder.GetHeight(x, y)) - centerHeight) <= 11;
        }

        private static List<int> GetHeights(Vector2i position, int sizeX, int sizeZ, BiomeType biome, int centerHeight)
        {
            var heights = new List<int>();

            for (int x = position.x; x < position.x + sizeX; x++)
            {
                for (int y = position.y; y < position.y + sizeZ; y++)
                {
                    if (!IsValidPosition(x, y, biome, centerHeight))
                    {
                        return null;
                    }

                    int height = (int)worldBuilder.GetHeight(x, y);

                    heights.Add(height);
                }
            }

            return heights;
        }

        private static int GetMedianHeight(List<int> heights)
        {
            var sortedHeights = new List<int>(heights);
            sortedHeights.Sort();

            return sortedHeights[sortedHeights.Count / 2];
        }

        private static (int, int) GetRotatedSizes(PrefabData wildernessPrefab, int rotation)
        {
            var sizeX = wildernessPrefab.size.x;
            var sizeZ = wildernessPrefab.size.z;

            if (rotation == 1 || rotation == 3)
            {
                sizeX = wildernessPrefab.size.z;
                sizeZ = wildernessPrefab.size.x;
            }

            return (sizeX, sizeZ);
        }

        private static Vector2i GetRandomPosition(GameRandom gameRandom, int sizeX, int sizeZ, int minSize, StreetTile streetTile, int offset)
        {
            if (sizeX > minSize || sizeZ > minSize)
                return streetTile.WorldPositionCenter - new Vector2i((sizeX - minSize) / 2, (sizeZ - minSize) / 2);

            try
            {
                return new Vector2i(
                    gameRandom.RandomRange(streetTile.WorldPosition.x, streetTile.WorldPosition.x + sizeX),
                    gameRandom.RandomRange(streetTile.WorldPosition.y, streetTile.WorldPosition.y + sizeZ)
                );
            }
            catch
            {
                return streetTile.WorldPositionCenter - new Vector2i(sizeX / 2, sizeZ / 2);
            }
        }

        private static Rect GetRect(Vector2i position, int sizeX, int sizeZ)
        {
            var size = Math.Max(sizeX, sizeZ);
            var rect = new Rect(position.x, position.y, size, size);
            var offset = new Vector2(size, size) / 2f;

            rect.min -= offset;
            rect.size += offset;
            rect.center = new Vector2(position.x + sizeZ / 2, position.y + sizeX / 2);

            return rect;
        }

        private static bool IsValidRect(Rect rect)
        {
            var worldSize = worldBuilder.WorldSize;

            return rect.max.x < worldSize && rect.min.x >= 0 && rect.max.y < worldSize && rect.min.y >= 0;
        }

        private static bool TrySpawnCaveEntrance(PrefabData prefabData, StreetTile streetTile, int prefabId)
        {
            int seed = worldBuilder.Seed + 468372;
            int MaxAttempts = 20;
            int MinSize = 150;
            int offset = 10;

            GameRandom gameRandom = GameRandomManager.Instance.CreateGameRandom(seed);

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var rotation = 0; // TODO: prefabData.RotationsToNorth + gameRandom.RandomRange(0, 4);
                var (sizeX, sizeZ) = GetRotatedSizes(prefabData, rotation);
                var position = GetRandomPosition(gameRandom, sizeX, sizeZ, MinSize, streetTile, offset);
                var rect = GetRect(position, sizeX, sizeZ);

                if (!IsValidRect(rect))
                {
                    continue;
                }

                var biome = worldBuilder.GetBiome((int)rect.center.x, (int)rect.center.y);
                var centerHeight = Mathf.CeilToInt(worldBuilder.GetHeight((int)rect.center.x, (int)rect.center.y));
                var heights = GetHeights(position, sizeX, sizeZ, biome, centerHeight);

                if (heights == null)
                {
                    continue;
                }

                var medianHeight = GetMedianHeight(heights);

                if (medianHeight + prefabData.yOffset < 2)
                {
                    continue;
                }


                var worldSize = worldBuilder.WorldSize;
                var prefabHeight = medianHeight + prefabData.yOffset + 1;
                var prefabPosition = new Vector3i(position.x, prefabHeight, position.y) - new Vector3i(worldSize / 2, 0, worldSize / 2);

                Log.Out($"[Cave] position = {position}");
                Log.Out($"[Cave] size = {sizeX}, {sizeZ}");
                Log.Out($"[Cave] prefabPosition = {prefabPosition}");
                Log.Out($"[Cave] add entrance at {prefabPosition}, medianHeight={medianHeight}");

                PrefabDataInstance pdi = new PrefabDataInstance(prefabId, new Vector3i(prefabPosition), (byte)rotation, prefabData);
                PrefabManager.AddUsedPrefabWorld(-1, pdi);

                // streetTile.SpawnMarkerPartsAndPrefabsWilderness(prefabData, prefabPosition, rotation);
                // streetTile.AddPrefab(pdi);
                // worldBuilder.WildernessPrefabCount++;

                GameRandomManager.Instance.FreeGameRandom(gameRandom);
                return true;
            }

            return false;
        }

        public static IEnumerator AddCaveEntrances(int count)
        {
            Log.Out("Start adding cave entrances");
            Log.Out($"[Cave] UnusedWildernessTiles = {WildernessPlanner.GetUnusedWildernessTiles().Count}");

            yield return null;

            List<PrefabData> caveEntrances = GetCaveEntrancePrefabs();
            List<StreetTile> streetTiles = WildernessPlanner.GetUnusedWildernessTiles();

            int maxPrefabID = PrefabManager.UsedPrefabsWorld.Count + 1;

            if (caveEntrances.Count == 0)
            {
                Log.Out($"[Cave] No cave entrance found.");
                yield break;
            }

            for (int i = 0; i < Utils.FastMin(count, streetTiles.Count); i++)
            {
                PrefabData prefab = caveEntrances[i % caveEntrances.Count];
                StreetTile streetTile = streetTiles[i % streetTiles.Count];

                if (TrySpawnCaveEntrance(prefab, streetTile, maxPrefabID++))
                    yield return null;

                yield return null;
            }

            // CaveBuilder.MAP_SIZE = worldBuilder.WorldSize;
            // int entrancesCount = 0;
            // List<PrefabWrapper> others = PrefabManager.UsedPrefabsWorld.Select(item => new PrefabWrapper(item)).ToList();
            // while (entrancesCount < count && entrancesCount < 2 * ++entrancesCount)
            // {
            //     int index = entrancesCount % caveEntrances.Count;
            //     var prefab = caveEntrances[index];
            //     var wrapper = new PrefabWrapper(others.Count + 1, prefab);

            //     if (!CaveBuilder.TryPlacePrefab(ref wrapper, others))
            //         continue;

            //     var prefabCenter = wrapper.GetCenter();
            //     var biome = worldBuilder.GetBiome(prefabCenter.x, prefabCenter.z);
            //     var centerHeight = Mathf.CeilToInt(worldBuilder.GetHeight(prefabCenter.x, prefabCenter.z));
            //     var heights = GetHeights(wrapper, biome, centerHeight);

            //     if (heights == null)
            //         continue;

            //     var medianHeight = GetMedianHeight(heights);

            //     if (medianHeight + wrapper.prefabDataInstance.prefab.yOffset < 2)
            //         continue;

            //     PrefabManager.AddUsedPrefabWorld(-1, wrapper.ToPrefabDataInstance(medianHeight - wrapper.prefabDataInstance.prefab.yOffset));


            //     worldBuilder.GetStreetTileWorld(new Vector2i(wrapper.position.x, wrapper.position.z)).WildernessPOISize = 10;


            //     Log.Out($"[Cave] cave entrance added at {wrapper.position}");
            //     entrancesCount++;
            //     yield return null;
            // }
            // Log.Warning($"[Cave] {entrancesCount} cave entrance added");

            yield return null; ;
        }



        public static IEnumerator GenerateData()
        {
            yield return worldBuilder.Init();
            yield return worldBuilder.SetMessage(string.Format(Localization.Get("xuiWorldGenerationGenerating"), worldBuilder.WorldName), _logToConsole: true);
            yield return worldBuilder.generateTerrain();

            if (worldBuilder.IsCanceled)
            {
                yield break;
            }

            worldBuilder.initStreetTiles();

            if (worldBuilder.IsCanceled)
            {
                yield break;
            }
            if (worldBuilder.Towns != 0 || worldBuilder.Wilderness != 0)
            {
                yield return PrefabManager.LoadPrefabs();
                PrefabManager.ShufflePrefabData(worldBuilder.Seed);
                yield return null;
                PathingUtils.SetupPathingGrid();
            }
            else
            {
                PrefabManager.ClearDisplayed();
            }
            if (worldBuilder.Towns != 0)
            {
                yield return TownPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
            }
            yield return worldBuilder.GenerateTerrainLast();
            if (worldBuilder.IsCanceled)
            {
                yield break;
            }
            yield return POISmoother.SmoothStreetTiles();
            if (worldBuilder.IsCanceled)
            {
                yield break;
            }
            if (worldBuilder.Towns != 0 || worldBuilder.Wilderness != 0)
            {
                yield return HighwayPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
                yield return TownPlanner.SpawnPrefabs();
                if (worldBuilder.IsCanceled)
                {
                    yield break;
                }
            }
            if (worldBuilder.Wilderness != 0)
            {
                yield return WildernessPlanner.Plan(worldBuilder.thisWorldProperties, worldBuilder.Seed);
                yield return worldBuilder.smoothWildernessTerrain();
                yield return WildernessPathPlanner.Plan(worldBuilder.Seed);
            }

            int num = 12 - worldBuilder.playerSpawns.Count;
            if (num > 0)
            {
                foreach (StreetTile item in WorldBuilder.CalcPlayerSpawnTiles())
                {
                    if (worldBuilder.CreatePlayerSpawn(item.WorldPositionCenter, _isFallback: true) && --num <= 0)
                    {
                        break;
                    }
                }
            }
            GC.Collect();
            yield return worldBuilder.SetMessage("Draw Roads", _logToConsole: true);
            yield return worldBuilder.DrawRoads(worldBuilder.dest);
            if (worldBuilder.Towns != 0 || worldBuilder.Wilderness != 0)
            {
                yield return worldBuilder.SetMessage("Smooth Road Terrain", _logToConsole: true);
                yield return WorldBuilder.smoothRoadTerrain(worldBuilder.dest, worldBuilder.HeightMap, worldBuilder.WorldSize);
            }

            yield return AddCaveEntrances(20);

            worldBuilder.paths.Clear();
            worldBuilder.wildernessPaths.Clear();
            yield return worldBuilder.FinalizeWater();
            GC.Collect();
            Log.Out("RWG final in {0}:{1:00}, r={2:x}", worldBuilder.totalMS.Elapsed.Minutes, worldBuilder.totalMS.Elapsed.Seconds, Rand.Instance.PeekSample());
        }

        public static bool Prefix(WorldBuilder __instance, ref IEnumerator __result)
        {
            Log.Out($"[Cave] start WorldBuilder_GenerateData Prefix. {CaveBuilder.SEED}");

            worldBuilder = __instance;

            __result = GenerateData();
            return false;
        }
    }
}
