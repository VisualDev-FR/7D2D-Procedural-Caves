using System.Linq;
using HarmonyLib;
using UnityEngine;
using static AIDirectorBloodMoonParty;


[HarmonyPatch(typeof(AIDirectorBloodMoonParty), "SpawnZombie")]
public class AIDirectorBloodMoonParty_SpawnZombie
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<AIDirectorBloodMoonParty_SpawnZombie>();

    private static readonly System.Random random = new System.Random();

    private static World World => GameManager.Instance.World;

    private static EntityPlayer Target { get; set; }

    private static AIDirectorBloodMoonParty Instance { get; set; }

    public static bool Prefix(ref AIDirectorBloodMoonParty __instance, World _world, EntityPlayer _target, Vector3 _focusPos, Vector3 _radiusV, ref bool __result)
    {
        Instance = __instance;
        Target = _target;

        if (!CaveGenerator.isEnabled || !CaveConfig.enableCaveBloodMoon)
            return true;

        if (RequirementIsInCave.IsInCavePrefab(_target))
        {
            __result = SpawnBloodMoonCaveZombie(GetSpawnPosNearPrefab(_target.prefab));
            return false;
        }

        if (RequirementIsInCave.IsInCave(_target))
        {
            __result = SpawnBloodMoonCaveZombie(GetSpawnPos(_target));
            return false;
        }

        return true;
    }

    private static Vector3i GetSpawnPosNearPrefab(PrefabInstance prefabInstance)
    {
        var markers = CaveUtils.GetCaveMarkers(prefabInstance).ToArray();
        var marker = markers[random.Next(markers.Length)];

        return CaveSpawnManager.GetSpawnPositionNearPlayer(marker.start + marker.size / 2, CaveConfig.minSpawnDist);
    }

    private static Vector3i GetSpawnPos(EntityPlayer player)
    {
        return CaveSpawnManager.GetSpawnPositionNearPlayer(player.position, CaveConfig.minSpawnDistBloodMoon);
    }

    private static bool SpawnBloodMoonCaveZombie(Vector3i spawnPos)
    {
        if (spawnPos == Vector3i.zero)
        {
            logger.Debug($"no spawn found from {spawnPos}");
            return false;
        }

        int et = EntityGroups.GetRandomFromGroup(Instance.partySpawner.spawnGroupName, ref Instance.lastClassId);

        EntityEnemy entityEnemy = (EntityEnemy)EntityFactory.CreateEntity(et, spawnPos);
        World.SpawnEntityInWorld(entityEnemy);
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

        ManagedZombie managedZombie = new ManagedZombie(entityEnemy, Target);
        Instance.zombies.Add(managedZombie);
        Instance.SeekTarget(managedZombie);
        Instance.partySpawner.IncSpawnCount();

        AstarManager.Instance.AddLocation(spawnPos, 40);
        var (day, hour, minute) = GameUtils.WorldTimeToElements(World.worldTime);

        logger.Info($"SpawnZombie grp {Instance.partySpawner}, cnt {Instance.zombies.Count}, {entityEnemy.EntityName}, loot {entityEnemy.lootDropProb}, at player {Target.entityId}, day/time {day} {hour:D2}:{minute:D2}");
        return true;
    }
}
