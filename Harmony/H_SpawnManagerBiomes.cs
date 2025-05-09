using HarmonyLib;
using UnityEngine;


public static class CaveSpawnTracker
{
    public static Dictionary<EntityPlayer, Vector3i> lastPlayerPositions = new Dictionary<EntityPlayer, Vector3i>();
    public static Dictionary<EntityPlayer, List<Vector3i>> playerMovementHistory = new Dictionary<EntityPlayer, List<Vector3i>>();
    public static HashSet<Vector3i> recentSpawnPositions = new HashSet<Vector3i>();

    public static int minPlayerMovementThreshold = 5; // Minimum movement to trigger a new spawn
    public static int movementTrackingLimit = 5; // How many recent positions to store
    public static int maxTrackedSpawns = 15; // Limit tracked spawn locations

    public static bool HasPlayerMovedSignificantly(EntityPlayer player, Vector3i currentPos)
    {
        if (!playerMovementHistory.ContainsKey(player))
        {
            playerMovementHistory[player] = new List<Vector3i>();
        }

        List<Vector3i> history = playerMovementHistory[player];

        // Add current position to history
        history.Add(currentPos);

        // Limit history size
        if (history.Count > movementTrackingLimit)
        {
            history.RemoveAt(0); // Remove oldest position
        }

        // Compute total travel distance without backtracking
        float totalDistance = 0f;
        for (int i = 1; i < history.Count; i++)
        {
            totalDistance += RebirthUtilities.Vector3iDistance(history[i - 1], history[i]);
        }

        // Check if movement is significant
        if (totalDistance > minPlayerMovementThreshold)
        {
            playerMovementHistory[player].Clear(); // Reset history after significant movement
            return true;
        }

        return false;
    }
}

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

        Vector3i playerPosition = new Vector3i(player.GetPosition());

        // Ensure the player has moved significantly
        if (!CaveSpawnTracker.HasPlayerMovedSignificantly(player, playerPosition))
        {
            return false;
        }

        if (playerPosition.y + CaveConfig.zombieSpawnMarginDeep > world.GetTerrainHeight(playerPosition.x, playerPosition.z))
        {
            return true;
        }

        Vector3i spawnPosition = CaveSpawnManager.GetSpawnPositionNearPlayer(playerPosition, CaveConfig.minSpawnDist);

        if (spawnPosition == Vector3.zero || CaveSpawnTracker.recentSpawnPositions.Contains(spawnPosition))
        {
            return false;
        }

        // Limit tracked spawn locations
        if (CaveSpawnTracker.recentSpawnPositions.Count >= CaveSpawnTracker.maxTrackedSpawns)
        {
            CaveSpawnTracker.recentSpawnPositions.Clear();
        }

        CaveSpawnTracker.recentSpawnPositions.Add(spawnPosition);

        int minDistance = 25;
        int minHeight = 15;
        List<Entity> entitiesInBounds = player.world.GetEntitiesInBounds(typeof(EntityZombie),
            BoundsUtils.BoundsForMinMax(player.position.x - minDistance, player.position.y - minHeight, player.position.z - minDistance,
                                        player.position.x + minDistance, player.position.y + minHeight, player.position.z + minDistance),
            new List<Entity>());

        int random = Manager.random.RandomRange(0, 101);
        float distance = RebirthUtilities.Vector3iDistance(playerPosition, spawnPosition);

        if (distance < 15 || entitiesInBounds.Count > 4 || random < 20)
        {
            return false;
        }

        int randomFromGroup = EntityGroups.GetRandomFromGroup("ZombiesCaveDay", ref spawnManagerBiome.lastClassId);
        if (randomFromGroup == 0)
        {
            return false;
        }

        EntityAlive entity = EntityFactory.CreateEntity(randomFromGroup, spawnPosition) as EntityAlive;
        entity.SetSpawnerSource(EnumSpawnerSource.Dynamic);

        world.SpawnEntityInWorld(entity);
        world.DebugAddSpawnedEntity(entity);
        entity.Buffs.SetCustomVar("$spawnedDescent", 1f);

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
