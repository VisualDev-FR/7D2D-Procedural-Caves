using System;
using System.Collections.Generic;
using System.Numerics;
using GamePath;
using UnityEngine;

public class EAIEatBlock : EAIBase
{
    private struct BlockTargetData
    {
        public static readonly BlockTargetData Null = default;

        public BlockValue blockValue;

        public Vector3 position;

        public string BlockName => blockValue.Block.blockName;

        public Vector3 getBellyPosition() => position;

        public BlockTargetData(Vector3 position)
        {
            position.x += 0.5f;
            position.y += 0.2f;
            position.z += 0.5f;

            this.position = position;
            this.blockValue = GameManager.Instance.World.GetBlock(new Vector3i(position));
        }
    }

    public const float cDamageBoostPerAlly = 0.2f;

    public int attackDelay;

    public float damageBoostPercent;

    public List<Entity> allies = new List<Entity>();

    private BlockTargetData entityTarget;

    private readonly int searchRadius = 10;

    private List<BlockValue> targetClasses;

    private int attackTimeout;

    private int eatCount;

    private bool isEating;

    private int pathCounter;

    public Vector2 seekPosOffset;

    public override void Init(EntityAlive _theEntity)
    {
        base.Init(_theEntity);
        MutexBits = 8;
        executeDelay = 0.15f;
    }

    public override void SetData(DictionarySave<string, string> data)
    {
        base.SetData(data);

        targetClasses = new List<BlockValue>();

        if (!data.TryGetValue("class", out var _value))
        {
            return;
        }
    }

    public override bool CanExecute()
    {

        if (theEntity.sleepingOrWakingUp || theEntity.bodyDamage.CurrentStun != 0 || (theEntity.Jumping && !theEntity.isSwimming))
        {
            Log.Out($"[Cave] EAIEatBlock: Can't Execute");
            return false;
        }

        entityTarget = FindBlockToEat(searchRadius);
        if (entityTarget.position == Vector3i.zero)
        {
            return false;
        }

        // Type type = blockTargetPosition.GetType();
        // for (int i = 0; i < targetClasses.Count; i++)
        // {
        //     TargetClass targetClass = targetClasses[i];
        //     if (targetClass.type != null && targetClass.type.IsAssignableFrom(type))
        //     {
        //         chaseTimeMax = targetClass.chaseTimeMax;
        //         return true;
        //     }
        // }
        return true;
    }

    public override void Start()
    {
        /* EAIBreakBlock */
        // attackDelay = 1;
        // Vector3i blockPos = theEntity.moveHelper.HitInfo.hit.blockPos;
        // Block block = theEntity.world.GetBlock(blockPos).Block;
        // if (block.HasTag(BlockTags.Door) || block.HasTag(BlockTags.ClosetDoor))
        // {
        //     theEntity.IsBreakingDoors = true;
        // }

        /* EAIApproachAndAttackTarget */
        // entityTargetPos = entityTarget.position;
        // entityTargetVel = Vector3.zero;
        // isTargetToEat = entityTarget.IsDead();
        isEating = false;
        theEntity.IsEating = false;
        // homeTimeout = (theEntity.IsSleeper ? 90f : chaseTimeMax);
        // hasHome = homeTimeout > 0f;
        // isGoingHome = false;
        // if (theEntity.ChaseReturnLocation == Vector3.zero)
        // {
        // 	theEntity.ChaseReturnLocation = (theEntity.IsSleeper ? theEntity.SleeperSpawnPosition : theEntity.position);
        // }
        // pathCounter = 0;
        // relocateTicks = 0;
        attackTimeout = 5;
        eatCount = 0;
    }

    public override bool Continue()
    {
        if (theEntity.bodyDamage.CurrentStun != 0 || !theEntity.onGround)
            return false;

        return CanExecute();
    }

    public override void Reset()
    {
        theEntity.IsEating = false;
        theEntity.IsBreakingBlocks = false;
        theEntity.IsBreakingDoors = false;
    }

    public override void Update()
    {
        var isTargetToEat = true;
        Vector3 entityTargetPos = entityTarget.position;
        attackTimeout--;

        if (isEating)
        {
            if (theEntity.bodyDamage.HasLimbs)
            {
                theEntity.RotateTo(entityTargetPos.x, entityTargetPos.y, entityTargetPos.z, 8f, 5f);
            }
            if (attackTimeout <= 0)
            {
                attackTimeout = 25 + GetRandom(10);
                if ((eatCount & 1) == 0)
                {
                    theEntity.PlayOneShot("eat_player");
                    if (DestroyBlock(entityTarget.position, 35))
                    {
                        isEating = false;
                        theEntity.IsEating = false;
                    }
                }
                Vector3 pos = new Vector3(0f, 0.04f, 0.08f);
                ParticleEffect pe = new ParticleEffect("blood_eat", pos, 1f, Color.white, null, theEntity.entityId, ParticleEffect.Attachment.Head);
                GameManager.Instance.SpawnParticleEffectServer(pe, theEntity.entityId);
                eatCount++;
            }
            return;
        }

        theEntity.moveHelper.CalcIfUnreachablePos();
        float entityHeight = theEntity.GetHeight() * 0.9f;
        float num2 = entityHeight - 0.05f;
        float sqrMaxDist = num2 * num2;

        float sqrTargetDistance = sqrTargetDistanceZX();
        float dy = entityTargetPos.y - theEntity.position.y;
        float dyAbs = Utils.FastAbs(dy);

        bool flag = sqrTargetDistance <= sqrMaxDist && dyAbs < 1f;
        if (!flag)
        {
            if (dyAbs < 3f && !PathFinderThread.Instance.IsCalculatingPath(theEntity.entityId))
            {
                PathEntity path = theEntity.navigator.getPath();
                if (path != null && path.NodeCountRemaining() <= 2)
                {
                    pathCounter = 0;
                }
            }
            if (--pathCounter <= 0 && theEntity.CanNavigatePath() && !PathFinderThread.Instance.IsCalculatingPath(theEntity.entityId))
            {
                pathCounter = 6 + GetRandom(10);
                Vector3 moveToLocation = GetMoveToLocation(num2);
                if (moveToLocation.y - theEntity.position.y < -8f)
                {
                    pathCounter += 40;
                    if (base.RandomFloat < 0.2f)
                    {
                        seekPosOffset.x += base.RandomFloat * 0.6f - 0.3f;
                        seekPosOffset.y += base.RandomFloat * 0.6f - 0.3f;
                    }
                    moveToLocation.x += seekPosOffset.x;
                    moveToLocation.z += seekPosOffset.y;
                }
                else
                {
                    float num7 = (moveToLocation - theEntity.position).magnitude - 5f;
                    if (num7 > 0f)
                    {
                        if (num7 > 60f)
                        {
                            num7 = 60f;
                        }
                        pathCounter += (int)(num7 / 20f * 20f);
                    }
                }
                theEntity.FindPath(moveToLocation, theEntity.moveSpeedAggro, canBreak: true, this);
            }
        }

        if (theEntity.Climbing)
        {
            return;
        }

        // bool flag2 = theEntity.CanSee(entityTarget);
        // theEntity.SetLookPosition((flag2 && !theEntity.IsBreakingBlocks) ? entityTarget.getHeadPosition() : Vector3.zero);

        if (!flag)
        {
            if (theEntity.navigator.noPathAndNotPlanningOne() && pathCounter > 0 && dy < 2.1f)
            {
                Vector3 moveToLocation2 = GetMoveToLocation(num2);
                theEntity.moveHelper.SetMoveTo(moveToLocation2, _canBreakBlocks: true);
            }
        }
        else
        {
            theEntity.moveHelper.Stop();
            pathCounter = 0;
        }

        float sqrEntityHeight = entityHeight * entityHeight;
        if (!(sqrTargetDistance <= sqrEntityHeight) || !(dyAbs < 1.25f))
        {
            return;
        }

        theEntity.IsBreakingBlocks = false;
        theEntity.IsBreakingDoors = false;

        if (theEntity.bodyDamage.HasLimbs && !theEntity.Electrocuted)
        {
            theEntity.RotateTo(entityTargetPos.x, entityTargetPos.y, entityTargetPos.z, 30f, 30f);
        }

        if (isTargetToEat)
        {
            isEating = true;
            theEntity.IsEating = true;
            attackTimeout = 20;
            eatCount = 0;
        }
    }

    public float sqrTargetDistanceZX()
    {
        return CaveUtils.SqrEuclidianDist(theEntity.position, entityTarget.position);
    }

    public Vector3 GetMoveToLocation(float maxDist)
    {
        var world = GameManager.Instance.World;
        var pos = entityTarget.position;

        pos = entityTarget.getBellyPosition();
        pos = world.FindSupportingBlockPos(pos);

        if (maxDist <= 0f)
            return pos;

        Vector3 vector = new Vector3(theEntity.position.x, pos.y, theEntity.position.z);
        Vector3 vector2 = pos - vector;
        float magnitude = vector2.magnitude;
        if (magnitude < 3f)
        {
            if (magnitude <= maxDist)
            {
                float num = pos.y - theEntity.position.y;
                if (num < -3f || num > 1.5f)
                {
                    return pos;
                }
                return vector;
            }
            vector2 *= maxDist / magnitude;
            Vector3 vector3 = pos - vector2;
            vector3.y += 0.51f;
            Vector3i pos2 = World.worldToBlockPos(vector3);
            BlockValue block = world.GetBlock(pos2);
            Block block2 = block.Block;

            if (block2.PathType <= 0)
            {
                // throw new NotImplementedException("Need to import Physics.Raycast");
                // if (Physics.Raycast(vector3 - Origin.position, Vector3.down, out var hitInfo, 1.02f, 1082195968))
                // {
                //     vector3.y = hitInfo.point.y + Origin.position.y;
                //     return vector3;
                // }
                // if (block2.IsElevator(block.rotation))
                // {
                //     vector3.y = pos.y;
                //     return vector3;
                // }
            }
        }

        return pos;
    }

    private bool DestroyBlock(Vector3 position, int amount)
    {
        var worldPos = new Vector3i(position);
        var wasDestroyed = false;
        var world = GameManager.Instance.World;
        var blockValue = world.GetBlock(worldPos);

        blockValue.damage += amount;

        if (blockValue.damage >= blockValue.Block.MaxDamage)
        {
            blockValue = BlockValue.Air;
            wasDestroyed = true;
        }

        world.SetBlockRPC(worldPos, blockValue);

        return wasDestroyed;
    }

    private BlockTargetData FindBlockToEat(int radius)
    {
        var timer = CaveUtils.StartTimer();
        var world = GameManager.Instance.World;
        var queue = new Queue<Vector3i>();
        var visited = new HashSet<Vector3i>();
        var start = new Vector3i(theEntity.position);
        var rolls = 0;

        queue.Enqueue(start);

        while (queue.Count > 0 && rolls++ < 100)
        {
            Vector3i currentPos = queue.Dequeue();

            if (CanEatBlockAt(currentPos))
            {
                Log.Out($"[Cave] Block to eat found at '{currentPos}', rolls: {rolls}, timer: {timer.ElapsedMilliseconds}ms");
                return new BlockTargetData(currentPos);
            }

            visited.Add(currentPos);

            foreach (var offset in CaveUtils.offsetsNoVertical)
            {
                Vector3i neighborPos = currentPos + offset;

                uint block = world.GetBlock(neighborPos).rawData;
                bool canExtend =
                       !visited.Contains(neighborPos)
                    && (block == 0 || block > 255)
                    && CaveBlocks.IsTerrain(world.GetBlock(neighborPos + Vector3i.down));

                if (!canExtend)
                    continue;

                queue.Enqueue(neighborPos);
            }
        }

        return BlockTargetData.Null;
    }

    private bool CanEatBlockAt(Vector3i position)
    {
        return GameManager.Instance.World.GetBlock(position).Block.blockName.StartsWith("goreBlock");
    }
}
