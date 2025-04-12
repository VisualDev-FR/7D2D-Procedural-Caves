using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;


public static class CaveSpawnTracker
{
    public static Dictionary<EntityPlayer, Vector3i> lastPlayerPositions = new Dictionary<EntityPlayer, Vector3i>();

    public static Dictionary<EntityPlayer, List<Vector3i>> playerMovementHistory = new Dictionary<EntityPlayer, List<Vector3i>>();

    public static HashSet<int> recentSpawnChunks = new HashSet<int>();

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
            totalDistance += FastMath.EuclidianDist(history[i - 1], history[i]);
        }

        // Check if movement is significant
        if (totalDistance > minPlayerMovementThreshold)
        {
            playerMovementHistory[player].Clear(); // Reset history after significant movement
            return true;
        }

        return false;
    }

    public static void AddSpawnPosition(Vector3i position)
    {
        var chunkHash = BFSUtils.PositionHashCode(
            position.x << 4,
            position.y << 4,
            position.z << 4
        );

        recentSpawnChunks.Add(chunkHash);
    }

    public static bool HasRecentSpawnAt(Vector3i position)
    {
        var chunkHash = BFSUtils.PositionHashCode(
            position.x << 4,
            position.y << 4,
            position.z << 4
        );

        return recentSpawnChunks.Contains(chunkHash);
    }
}


[HarmonyPatch(typeof(SpawnManagerBiomes), "Update")]
public class SpawnManagerBiomes_Update
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<SpawnManagerBiomes_Update>();

    private static SpawnManagerBiomes spawnManagerBiome;

    private static readonly float spawnAreaSize = 40f;

    public static bool Prefix(SpawnManagerBiomes __instance, string _spawnerName, bool _bSpawnEnemyEntities, object _userData)
    {
        if (!GameManager.Instance.IsEditMode() && !GameUtils.IsPlaytesting() && CaveGenerator.isEnabled)
        {
            spawnManagerBiome = __instance;
            return SpawnUpdate(_bSpawnEnemyEntities, _userData as ChunkAreaBiomeSpawnData);
        }

        return true;
    }

    public static bool SpawnUpdate(bool _isSpawnEnemy, ChunkAreaBiomeSpawnData _spawnData)
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
        if (player is null || !RequirementIsInCave.IsInCave(player))
        {
            return true;
        }

        Vector3i playerPosition = new Vector3i(player.GetPosition());

        if (playerPosition.y + CaveConfig.zombieSpawnMarginDeep > world.GetTerrainHeight(playerPosition.x, playerPosition.z))
        {
            return true;
        }

        // Ensure the player has moved significantly
        if (!CaveSpawnTracker.HasPlayerMovedSignificantly(player, playerPosition))
        {
            return false;
        }

        Vector3i spawnPosition = CaveSpawnManager.GetSpawnPositionNearPlayer(playerPosition, CaveConfig.minSpawnDist);

        if (spawnPosition == Vector3.zero || CaveSpawnTracker.HasRecentSpawnAt(spawnPosition))
        {
            return false;
        }

        // Limit tracked spawn locations
        if (CaveSpawnTracker.recentSpawnChunks.Count >= CaveSpawnTracker.maxTrackedSpawns)
        {
            CaveSpawnTracker.recentSpawnChunks.Clear();
        }

        int minDistance = 25;
        int minHeight = 15;
        List<Entity> entitiesInBounds = player.world.GetEntitiesInBounds(typeof(EntityZombie),
            BoundsUtils.BoundsForMinMax(player.position.x - minDistance, player.position.y - minHeight, player.position.z - minDistance,
                                        player.position.x + minDistance, player.position.y + minHeight, player.position.z + minDistance),
            new List<Entity>());

        int random = new System.Random().Next(0, 101);
        float distance = FastMath.EuclidianDist(playerPosition, spawnPosition);

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

        logger.Info($"spawn '{entity.GetDebugName()}' at {spawnPosition}");

        CaveSpawnTracker.AddSpawnPosition(spawnPosition);

        return false;
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
