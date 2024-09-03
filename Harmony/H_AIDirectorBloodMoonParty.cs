using HarmonyLib;
using UnityEngine;


[HarmonyPatch(typeof(AIDirectorBloodMoonParty), "SpawnZombie")]
public class AIDirectorBloodMoonParty_SpawnZombie
{
    private static World world;

    private static EntityPlayer target;

    public static bool Prefix(World _world, EntityPlayer _target, Vector3 _focusPos, Vector3 _radiusV, ref bool __result)
    {
        if (!CaveGenerator.isEnabled)
            return true;

        if (_target.position.y > _world.GetHeight(_target.position.x, _target.position.z))
            return true;

        world = _world;
        target = _target;

        __result = SpawnBloodMoonCaveZombie();

        return false;
    }

    private static bool SpawnBloodMoonCaveZombie()
    {
        if (!CalcSpawnPos(_world, _focusPos, _radiusV, out var spawnPos))
        {
            return false;
        }
        bool flag = true;
        int et = EntityGroups.GetRandomFromGroup(partySpawner.spawnGroupName, ref lastClassId);
        if ((bool)_target.AttachedToEntity && controller.Random.RandomFloat < 0.5f)
        {
            flag = false;
            et = EntityClass.FromString("animalZombieVultureRadiated");
        }
        EntityEnemy entityEnemy = (EntityEnemy)EntityFactory.CreateEntity(et, spawnPos);
        _world.SpawnEntityInWorld(entityEnemy);
        entityEnemy.SetSpawnerSource(EnumSpawnerSource.Dynamic);
        entityEnemy.IsHordeZombie = true;
        entityEnemy.IsBloodMoon = true;
        entityEnemy.bIsChunkObserver = true;
        entityEnemy.timeStayAfterDeath /= 3;
        if (flag && ++bonusLootSpawnCount >= partySpawner.bonusLootEvery)
        {
            bonusLootSpawnCount = 0;
            entityEnemy.lootDropProb *= GameStageDefinition.LootBonusScale;
        }
        ManagedZombie managedZombie = new ManagedZombie(entityEnemy, _target);
        zombies.Add(managedZombie);
        SeekTarget(managedZombie);
        partySpawner.IncSpawnCount();
        AstarManager.Instance.AddLocation(spawnPos, 40);
        var (num, num2, num3) = GameUtils.WorldTimeToElements(_world.worldTime);
        Log.Out("BloodMoonParty: SpawnZombie grp {0}, cnt {1}, {2}, loot {3}, at player {4}, day/time {5} {6:D2}:{7:D2}", partySpawner.ToString(), zombies.Count, entityEnemy.EntityName, entityEnemy.lootDropProb, _target.entityId, num, num2, num3);
        return true;
    }
}
