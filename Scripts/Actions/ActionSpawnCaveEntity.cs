using System.Collections.Generic;
using Audio;
using GameEvent.SequenceActions;
using UnityEngine;


// We just need to override FindValidPosition, but it is a static method... so we copy most of the entire ActionSpawnEntity class... YOLO !
public class ActionSpawnCaveEntity : ActionSpawnEntity
{
    public override ActionCompleteStates OnPerformAction()
    {
        if (!Owner.HasTarget())
        {
            return ActionCompleteStates.InCompleteRefund;
        }
        HandleExtraAction();
        switch (CurrentState)
        {
            case SpawnUpdateTypes.NeedSpawnEntries:
                {
                    if (SpawnEntries != null)
                    {
                        break;
                    }
                    if (!useEntityGroup && entityIDs.Count == 0)
                    {
                        SetupEntityIDs();
                        return ActionCompleteStates.InComplete;
                    }
                    SpawnEntries = new List<SpawnEntry>();
                    if (singleChoice && selectedEntityIndex == -1)
                    {
                        selectedEntityIndex = Random.Range(0, entityIDs.Count);
                    }
                    GameStageDefinition gameStageDefinition = null;
                    int lastClassId = -1;
                    if (useEntityGroup)
                    {
                        gameStageDefinition = GameStageDefinition.GetGameStage(entityNames);
                        if (gameStageDefinition == null)
                        {
                            return ActionCompleteStates.InCompleteRefund;
                        }
                    }
                    if (targetGroup != "")
                    {
                        List<Entity> entityGroup = Owner.GetEntityGroup(targetGroup);
                        if (entityGroup != null)
                        {
                            for (int k = 0; k < entityGroup.Count; k++)
                            {
                                if (!(entityGroup[k] is EntityPlayer entityPlayer) || (Owner.ActionType == GameEventActionSequence.ActionTypes.TwitchAction && entityPlayer.TwitchActionsEnabled != EntityPlayer.TwitchActionsStates.Enabled))
                                {
                                    continue;
                                }
                                int num = ModifiedCount(entityPlayer);
                                _ = GameManager.Instance.World;
                                for (int l = 0; l < num; l++)
                                {
                                    if (useEntityGroup)
                                    {
                                        int randomFromGroup = EntityGroups.GetRandomFromGroup(gameStageDefinition.GetStage(entityPlayer.PartyGameStage).GetSpawnGroup(0).groupName, ref lastClassId);
                                        SpawnEntries.Add(new SpawnEntry
                                        {
                                            EntityTypeID = randomFromGroup,
                                            Target = entityPlayer
                                        });
                                    }
                                    else
                                    {
                                        int index = ((selectedEntityIndex == -1) ? Random.Range(0, entityIDs.Count) : selectedEntityIndex);
                                        SpawnEntries.Add(new SpawnEntry
                                        {
                                            EntityTypeID = entityIDs[index],
                                            Target = entityPlayer
                                        });
                                    }
                                }
                                if (attackTarget)
                                {
                                    Owner.ReservedSpawnCount += num;
                                    GameEventManager.Current.ReservedCount += num;
                                }
                            }
                        }
                        else
                        {
                            int num2 = ModifiedCount(Owner.Target);
                            for (int m = 0; m < num2; m++)
                            {
                                if (useEntityGroup)
                                {
                                    if (base.Owner.Target is EntityPlayer entityPlayer2)
                                    {
                                        int randomFromGroup2 = EntityGroups.GetRandomFromGroup(gameStageDefinition.GetStage(entityPlayer2.PartyGameStage).GetSpawnGroup(0).groupName, ref lastClassId);
                                        SpawnEntries.Add(new SpawnEntry
                                        {
                                            EntityTypeID = randomFromGroup2,
                                            Target = entityPlayer2
                                        });
                                    }
                                }
                                else
                                {
                                    int index2 = ((selectedEntityIndex == -1) ? Random.Range(0, entityIDs.Count) : selectedEntityIndex);
                                    SpawnEntries.Add(new SpawnEntry
                                    {
                                        EntityTypeID = entityIDs[index2],
                                        Target = base.Owner.Target
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        int num3 = ModifiedCount(base.Owner.Target);
                        for (int n = 0; n < num3; n++)
                        {
                            if (useEntityGroup)
                            {
                                if (!(base.Owner.Target is EntityPlayer entityPlayer3))
                                {
                                    Debug.LogWarning("ActionBaseSpawn: Use EntityGroup requires a player target.");
                                    return ActionCompleteStates.InCompleteRefund;
                                }
                                int randomFromGroup3 = EntityGroups.GetRandomFromGroup(gameStageDefinition.GetStage(entityPlayer3.PartyGameStage).GetSpawnGroup(0).groupName, ref lastClassId);
                                SpawnEntries.Add(new SpawnEntry
                                {
                                    EntityTypeID = randomFromGroup3,
                                    Target = entityPlayer3
                                });
                            }
                            else
                            {
                                int index3 = ((selectedEntityIndex == -1) ? Random.Range(0, entityIDs.Count) : selectedEntityIndex);
                                SpawnEntries.Add(new SpawnEntry
                                {
                                    EntityTypeID = entityIDs[index3],
                                    Target = base.Owner.Target
                                });
                            }
                        }
                    }
                    CurrentState = SpawnUpdateTypes.NeedPosition;
                    break;
                }
            case SpawnUpdateTypes.NeedPosition:
                if (spawnType == SpawnTypes.NearTarget && base.Owner.Target == null && base.Owner.TargetPosition.y != 0f)
                {
                    spawnType = SpawnTypes.NearPosition;
                }
                switch (spawnType)
                {
                    case SpawnTypes.Position:
                        if (base.Owner.TargetPosition.y != 0f)
                        {
                            position = base.Owner.TargetPosition;
                            CurrentState = SpawnUpdateTypes.SpawnEntities;
                            resetTime = 3f;
                        }
                        else if (base.Owner.Target != null)
                        {
                            if (!FindValidPosition(out position, base.Owner.Target, minDistance, maxDistance, safeSpawn, yOffset, airSpawn))
                            {
                                return ActionCompleteStates.InComplete;
                            }
                            CurrentState = SpawnUpdateTypes.SpawnEntities;
                            resetTime = 3f;
                        }
                        else
                        {
                            spawnType = SpawnTypes.NearTarget;
                            CurrentState = SpawnUpdateTypes.SpawnEntities;
                        }
                        break;
                    case SpawnTypes.NearPosition:
                        if (base.Owner.TargetPosition.y != 0f)
                        {
                            position = base.Owner.TargetPosition;
                        }
                        else if (base.Owner.Target != null)
                        {
                            position = base.Owner.Target.position;
                        }
                        CurrentState = SpawnUpdateTypes.SpawnEntities;
                        break;
                    case SpawnTypes.NearTarget:
                        if (base.Owner.Target == null)
                        {
                            return ActionCompleteStates.InCompleteRefund;
                        }
                        position = base.Owner.Target.position;
                        CurrentState = SpawnUpdateTypes.SpawnEntities;
                        break;
                    case SpawnTypes.WanderingHorde:
                        CurrentState = SpawnUpdateTypes.SpawnEntities;
                        if (base.Owner.TargetPosition == Vector3.zero && base.Owner.Target != null)
                        {
                            base.Owner.TargetPosition = base.Owner.Target.position;
                        }
                        break;
                }
                break;
            case SpawnUpdateTypes.SpawnEntities:
                {
                    if (SpawnEntries.Count == 0)
                    {
                        if (UseRepeating)
                        {
                            if (HandleRepeat())
                            {
                                SpawnEntries = null;
                                CurrentState = SpawnUpdateTypes.NeedSpawnEntries;
                            }
                            return ActionCompleteStates.InComplete;
                        }
                        if (clearPositionOnComplete)
                        {
                            base.Owner.TargetPosition = Vector3.zero;
                        }
                        if (!hasSpawned)
                        {
                            return ActionCompleteStates.InCompleteRefund;
                        }
                        return ActionCompleteStates.Complete;
                    }
                    if (spawnType == SpawnTypes.Position)
                    {
                        resetTime -= Time.deltaTime;
                        if (resetTime <= 0f)
                        {
                            CurrentState = SpawnUpdateTypes.NeedPosition;
                            return ActionCompleteStates.InComplete;
                        }
                    }
                    for (int i = 0; i < SpawnEntries.Count; i++)
                    {
                        SpawnEntry spawnEntry = SpawnEntries[i];
                        if (spawnEntry.Target == null && spawnType != SpawnTypes.Position)
                        {
                            SpawnEntries.RemoveAt(i);
                            break;
                        }
                        Entity entity = null;
                        switch (spawnType)
                        {
                            case SpawnTypes.Position:
                                entity = SpawnEntity(spawnEntry.EntityTypeID, spawnEntry.Target, position, 1f, 4f, safeSpawn, yOffset);
                                break;
                            case SpawnTypes.NearTarget:
                                entity = SpawnEntity(spawnEntry.EntityTypeID, spawnEntry.Target, spawnEntry.Target.position, minDistance, maxDistance, safeSpawn, yOffset);
                                break;
                            case SpawnTypes.NearPosition:
                                entity = SpawnEntity(spawnEntry.EntityTypeID, spawnEntry.Target, position, minDistance, maxDistance, safeSpawn, yOffset);
                                break;
                            case SpawnTypes.WanderingHorde:
                                if (!GameManager.Instance.World.GetMobRandomSpawnPosWithWater(base.Owner.TargetPosition, (int)minDistance, (int)maxDistance, 15, _checkBedrolls: false, out position))
                                {
                                    return ActionCompleteStates.InComplete;
                                }
                                entity = SpawnEntity(spawnEntry.EntityTypeID, spawnEntry.Target, position, 1f, 1f, safeSpawn, yOffset);
                                break;
                        }
                        if (!(entity != null))
                        {
                            continue;
                        }
                        resetTime = 60f;
                        AddPropertiesToSpawnedEntity(entity);
                        base.Owner.TargetPosition = position;
                        if (AddToGroups != null)
                        {
                            for (int j = 0; j < AddToGroups.Length; j++)
                            {
                                if (AddToGroups[j] != "")
                                {
                                    base.Owner.AddEntityToGroup(AddToGroups[j], entity);
                                }
                            }
                        }
                        if (attackTarget && entity is EntityAlive attacker && base.Owner.Target is EntityAlive entityAlive)
                        {
                            HandleTargeting(attacker, entityAlive);
                            GameEventManager.Current.RegisterSpawnedEntity(entity, entityAlive, base.Owner.Requester, base.Owner, isAggressive);
                            base.Owner.ReservedSpawnCount--;
                            GameEventManager.Current.ReservedCount--;
                        }
                        if (base.Owner.Requester != null)
                        {
                            GameEventActionSequence gameEventActionSequence = ((base.Owner.OwnerSequence == null) ? base.Owner : base.Owner.OwnerSequence);
                            if (base.Owner.Requester is EntityPlayerLocal)
                            {
                                GameEventManager.Current.HandleGameEntitySpawned(gameEventActionSequence.Name, entity.entityId, gameEventActionSequence.Tag);
                            }
                            else
                            {
                                SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageGameEventResponse>().Setup(gameEventActionSequence.Name, gameEventActionSequence.Target.entityId, gameEventActionSequence.ExtraData, gameEventActionSequence.Tag, NetPackageGameEventResponse.ResponseTypes.EntitySpawned, entity.entityId), _onlyClientsAttachedToAnEntity: false, gameEventActionSequence.Requester.entityId);
                            }
                        }
                        hasSpawned = true;
                        SpawnEntries.RemoveAt(i);
                        break;
                    }
                    break;
                }
        }
        return ActionCompleteStates.InComplete;
    }

    public new bool FindValidPosition(out Vector3 newPoint, Entity entity, float minDistance, float maxDistance, bool spawnInSafe, float yOffset = 0f, bool spawnInAir = false)
    {
        return FindValidPosition(out newPoint, entity.position, minDistance, maxDistance, spawnInSafe, yOffset, spawnInAir);
    }

    // overrided method
    public new bool FindValidPosition(out Vector3 newPoint, Vector3 startPoint, float minDistance, float maxDistance, bool spawnInSafe, float yOffset = 0f, bool spawnInAir = false, float raycastOffset = 0f)
    {
        newPoint = CaveGenerator.caveChunksProvider.GetSpawnPositionNearPlayer(startPoint, minDistance);

        Log.Out($"[Cave] FindValidPosition: {newPoint}, startPoint: {startPoint}, minDistance: {minDistance}"); // \n{StackTraceUtility.ExtractStackTrace()}

        return newPoint != null;
    }

    public new Entity SpawnEntity(int spawnedEntityID, Entity target, Vector3 startPoint, float minDistance, float maxDistance, bool spawnInSafe, float yOffset = 0f)
    {
        World world = GameManager.Instance.World;
        Vector3 rotation = (target != null) ? new Vector3(0f, target.transform.eulerAngles.y + 180f, 0f) : Vector3.zero;
        Entity entity = null;

        //NOTE:  startPoint was replaced by target.position, to avoid searching from rayCasted startPoint
        if (FindValidPosition(out var newPoint, target.position, minDistance, maxDistance, spawnInSafe, yOffset, airSpawn, raycastOffset))
        {
            entity = EntityFactory.CreateEntity(spawnedEntityID, newPoint + new Vector3(0f, 0.5f, 0f), rotation, (target != null) ? target.entityId : (-1), base.Owner.ExtraData);
            entity.SetSpawnerSource(EnumSpawnerSource.Dynamic);
            world.SpawnEntityInWorld(entity);

            Log.Out($"[Cave] ActionSpawnCaveEntity: entity spawned at {entity.position}");

            if (target != null && spawnSound != "")
            {
                Manager.BroadcastPlayByLocalPlayer(entity.position, spawnSound);
            }
        }
        return entity;
    }

    public override BaseAction CloneChildSettings()
    {
        return new ActionSpawnCaveEntity
        {
            count = count,
            currentCount = currentCount,
            entityNames = entityNames,
            maxDistance = maxDistance,
            minDistance = minDistance,
            safeSpawn = safeSpawn,
            airSpawn = airSpawn,
            singleChoice = singleChoice,
            targetGroup = targetGroup,
            partyAdditionText = partyAdditionText,
            AddToGroup = AddToGroup,
            AddToGroups = AddToGroups,
            AddBuffs = AddBuffs,
            spawnType = spawnType,
            clearPositionOnComplete = clearPositionOnComplete,
            yOffset = yOffset,
            attackTarget = attackTarget,
            useEntityGroup = useEntityGroup,
            ignoreMultiplier = ignoreMultiplier,
            onlyTargetPlayers = onlyTargetPlayers,
            raycastOffset = raycastOffset,
            isAggressive = isAggressive,
            spawnSound = spawnSound
        };
    }
}