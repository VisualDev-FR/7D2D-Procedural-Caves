using HarmonyLib;
using UnityEngine;
using static AIDirectorBloodMoonParty;


[HarmonyPatch(typeof(AIDirectorBloodMoonParty), "SpawnZombie")]
public class AIDirectorBloodMoonParty_SpawnZombie
{
    private static World world;

    private static EntityPlayer target;

    private static AIDirectorBloodMoonParty Instance;

    private static readonly int minEnemySpawnDist = 40;

    public static bool Prefix(ref AIDirectorBloodMoonParty __instance, World _world, EntityPlayer _target, Vector3 _focusPos, Vector3 _radiusV, ref bool __result)
    {
        if (!CaveGenerator.isEnabled)
            return true;

        if (_target.position.y > _world.GetHeight((int)_target.position.x, (int)_target.position.z))
            return true;

        Instance = __instance;
        world = _world;
        target = _target;

        __result = SpawnBloodMoonCaveZombie();

        return false;
    }

    private static bool SpawnBloodMoonCaveZombie()
    {
        var spawnPositions = CaveGenerator.caveChunksProvider.GetSpawnPositionsFromPlayer(target.position, minEnemySpawnDist);

        if (spawnPositions.Count == 0)
        {
            Log.Error($"[Cave] no spawn position found for BloodMoonParty.");
        }
        var randomIndex = Instance.controller.Random.Next(spawnPositions.Count);
        var spawnPos = spawnPositions[randomIndex].ToWorldPos(CaveGenerator.HalfWorldSize);

        int et = EntityGroups.GetRandomFromGroup(Instance.partySpawner.spawnGroupName, ref Instance.lastClassId);

        EntityEnemy entityEnemy = (EntityEnemy)EntityFactory.CreateEntity(et, spawnPos);
        world.SpawnEntityInWorld(entityEnemy);
        entityEnemy.SetSpawnerSource(EnumSpawnerSource.Dynamic);
        entityEnemy.IsHordeZombie = true;
        entityEnemy.IsBloodMoon = true;
        entityEnemy.bIsChunkObserver = true;
        entityEnemy.timeStayAfterDeath /= 3;

        if (++Instance.bonusLootSpawnCount >= Instance.partySpawner.bonusLootEvery)
        {
            Instance.bonusLootSpawnCount = 0;
            entityEnemy.lootDropProb *= GameStageDefinition.LootBonusScale;
        }

        ManagedZombie managedZombie = new ManagedZombie(entityEnemy, target);
        Instance.zombies.Add(managedZombie);
        Instance.SeekTarget(managedZombie);
        Instance.partySpawner.IncSpawnCount();

        AstarManager.Instance.AddLocation(spawnPos, 40);
        var (day, hour, minute) = GameUtils.WorldTimeToElements(world.worldTime);

        Log.Out($"BloodMoonParty: SpawnZombie grp {Instance.partySpawner}, cnt {Instance.zombies.Count}, {entityEnemy.EntityName}, loot {entityEnemy.lootDropProb}, at player {target.entityId}, day/time {day} {hour:D2}:{minute:D2}");
        return true;
    }
}
