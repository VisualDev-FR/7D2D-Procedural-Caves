using System.Collections.Generic;

public class EAIEatBlock : EAIBase
{
    private struct BlockTargetData
    {
        public static readonly BlockTargetData Null = default;

        public BlockValue blockValue;

        public Vector3i position;

        public string BlockName => blockValue.Block.blockName;

        public BlockTargetData(Vector3i position)
        {
            this.position = position;
            this.blockValue = GameManager.Instance.World.GetBlock(position);
        }
    }

    public const float cDamageBoostPerAlly = 0.2f;

    public int attackDelay;

    public float damageBoostPercent;

    public List<Entity> allies = new List<Entity>();

    private BlockTargetData blockTargetDatas;

    private readonly int searchRadius = 10;

    private List<BlockValue> targetClasses;

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

        blockTargetDatas = FindBlockToEat(searchRadius);
        if (blockTargetDatas.position == Vector3i.zero)
        {
            Log.Out($"[Cave] EAIEatBlock: no block to eat found");
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
        return false;
    }

    // public override void Start()
    // {
    //     attackDelay = 1;
    //     Vector3i blockPos = theEntity.moveHelper.HitInfo.hit.blockPos;
    //     Block block = theEntity.world.GetBlock(blockPos).Block;
    //     if (block.HasTag(BlockTags.Door) || block.HasTag(BlockTags.ClosetDoor))
    //     {
    //         theEntity.IsBreakingDoors = true;
    //     }
    // }

    public override bool Continue()
    {
        if (theEntity.bodyDamage.CurrentStun != 0 || !theEntity.onGround)
            return false;

        return CanExecute();
    }

    public override void Update()
    {
        _ = theEntity.moveHelper;
        if (attackDelay > 0)
        {
            attackDelay--;
        }
        if (attackDelay <= 0)
        {
            AttackBlock();
        }
    }

    public override void Reset()
    {
        theEntity.IsEating = false;
        theEntity.IsBreakingBlocks = false;
        theEntity.IsBreakingDoors = false;
    }

    public void AttackBlock()
    {
        var targetPos = blockTargetDatas.position;

        theEntity.SetLookPosition(targetPos);
        theEntity.moveHelper.SetMoveTo(targetPos, _canBreakBlocks: false);

        Log.Out($"[Cave] move entity '{theEntity.entityId}' to {blockTargetDatas.position} ({blockTargetDatas.blockValue})");

        /* if (!(theEntity.inventory.holdingItemData.actionData[0] is ItemActionAttackData itemActionAttackData))
        {
            return;
        }

        if (theEntity.Climbing)
        {
            return;
        }

        if (theEntity.IsEating)
        {
            if (theEntity.bodyDamage.HasLimbs)
            {
                theEntity.RotateTo(vector2.x, vector2.y, vector2.z, 8f, 5f);
            }
            if (attackTimeout <= 0)
            {
                attackTimeout = 25 + GetRandom(10);
                if ((eatCount++ & 1) == 0)
                {
                    theEntity.PlayOneShot("eat_player");
                    blockTargetPosition.DamageEntity(DamageSource.eat, 35, _criticalHit: false);
                }
                Vector3 pos = new Vector3(0f, 0.04f, 0.08f);
                ParticleEffect pe = new ParticleEffect("blood_eat", pos, 1f, Color.white, null, theEntity.entityId, ParticleEffect.Attachment.Head);
                GameManager.Instance.SpawnParticleEffectServer(pe, theEntity.entityId);
            }

            theEntity.moveHelper.SetMoveTo(blockTargetPosition, _canBreakBlocks: false);

            if (isTargetToEat)
            {
                isEating = true;
                theEntity.IsEating = true;
                attackTimeout = 20;
                eatCount = 0;
                return;
            }
        }

        if (theEntity.Attack(_bAttackReleased: false))
        {
            theEntity.IsEating = true;
            theEntity.IsBreakingBlocks = true;
            float num2 = 0.25f + base.RandomFloat * 0.8f;
            if (theEntity.moveHelper.IsUnreachableAbove)
            {
                num2 *= 0.5f;
            }
            attackDelay = (int)((num2 + 0.75f) * 20f);
            itemActionAttackData.hitDelegate = GetHitInfo;
            theEntity.Attack(_bAttackReleased: true);
        } */
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
