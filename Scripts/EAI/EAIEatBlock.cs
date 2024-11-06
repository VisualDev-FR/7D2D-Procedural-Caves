using System.Collections.Generic;
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

        public Vector3 GetBellyPosition() => new Vector3(
            position.x + 0.5f,
            position.y + 0.2f,
            position.z + 0.5f
        );

        public BlockTargetData(Vector3 position)
        {
            this.position = position;
            this.blockValue = GameManager.Instance.World.GetBlock(new Vector3i(position));
        }
    }

    private BlockTargetData entityTarget;

    private int attackTimeout;

    private int eatCount;

    private bool isEating;

    private int pathCounter;

    public Vector2 seekPosOffset;

    private readonly HashSet<string> foodBlockNames = new HashSet<string>();

    public override void Init(EntityAlive _theEntity)
    {
        base.Init(_theEntity);
        MutexBits = 8;
        executeDelay = 0.15f;
    }

    public override void SetData(DictionarySave<string, string> data)
    {
        base.SetData(data);

        foodBlockNames.Clear();

        if (!data.TryGetValue("placeholder", out string placeholderName))
        {
            Log.Error($"[Cave] placeholder data missing for entity class '{theEntity.EntityClass.entityClassName}'");
            return;
        }

        foodBlockNames.UnionWith(GetPlaceholderBlocks(placeholderName));
    }

    public override bool CanExecute()
    {

        if (foodBlockNames.Count == 0 || theEntity.sleepingOrWakingUp || theEntity.bodyDamage.CurrentStun != 0 || (theEntity.Jumping && !theEntity.isSwimming))
        {
            return false;
        }

        entityTarget = FindBlockToEat();
        if (entityTarget.position == Vector3i.zero)
        {
            return false;
        }

        return true;
    }

    public override void Start()
    {
        isEating = false;
        theEntity.IsEating = false;
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
        Vector3 entityTargetPos = entityTarget.GetBellyPosition();
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

        float sqrTargetDistance = CaveUtils.SqrEuclidianDist(theEntity.position, entityTarget.GetBellyPosition());
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
                Vector3 moveToLocation = entityTarget.GetBellyPosition();
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
                Vector3 moveToLocation2 = entityTarget.GetBellyPosition();
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

    private bool DestroyBlock(Vector3 position, int amount)
    {
        var worldPos = new Vector3i(position);
        var wasDestroyed = false;
        var world = GameManager.Instance.World;
        var blockValue = world.GetBlock(worldPos);

        blockValue.damage += amount;

        if (blockValue.damage >= blockValue.Block.MaxDamage)
        {
            wasDestroyed = true;
            blockValue = BlockPlaceholderMap.Instance.Replace(
                CaveBlocks.bloodDecorPlaceholder,
                Random,
                (int)position.x,
                (int)position.z
            );
        }

        world.SetBlockRPC(worldPos, blockValue);

        return wasDestroyed;
    }

    private BlockTargetData FindBlockToEat()
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
        var blockName = GameManager.Instance.World.GetBlock(position).Block.blockName;
        return foodBlockNames.Contains(blockName);
        // return GameManager.Instance.World.GetBlock(position).Block.blockName.StartsWith("goreBlock");
    }

    private IEnumerable<string> GetPlaceholderBlocks(string placeholderName)
    {
        if (!Block.nameToBlockCaseInsensitive.TryGetValue(placeholderName, out var placeholder))
        {
            Log.Error($"[Cave] placeholder not found: '{placeholderName}'");
            yield break;
        }

        if (!BlockPlaceholderMap.Instance.placeholders.TryGetValue(placeholder.ToBlockValue(), out var placeholderTargets))
        {
            Log.Warning($"[Cave] placeholder '{placeholder.blockName}' not found!");
            yield break;
        }

        foreach (var target in placeholderTargets)
        {
            yield return target.block.Block.blockName;
        }
    }
}
