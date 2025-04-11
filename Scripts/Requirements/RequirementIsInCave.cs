public class RequirementIsInCave : TargetedCompareRequirementBase
{
    public override bool IsValid(MinEventParams _params)
    {
        if (!ParamsValid(_params) || !(_params.Self is EntityPlayer player))
        {
            return false;
        }

        bool isInCave = IsInCaveTunnel(player);

        return invert ? !isInCave : isInCave;
    }

    public static bool IsInCaveTunnel(EntityPlayer player, int radius = 2)
    {
        if (player.prefab != null) return false;

        return IsInCave(player, radius);
    }

    // Credits: https://github.com/FuriousRamsay
    public static bool IsInCave(EntityPlayer player, int radius = 2)
    {
        World world = GameManager.Instance.World;

        float playerPosY = player.position.y + CaveConfig.zombieSpawnMarginDeep;
        float terrainHeight = world.GetHeight((int)player.position.x, (int)player.position.z);

        if (playerPosY >= terrainHeight) return false;

        Vector3i playerPos = new Vector3i(player.position);
        Vector3i checkPos = Vector3i.zero;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    checkPos.x = playerPos.x + x;
                    checkPos.y = playerPos.y + y;
                    checkPos.z = playerPos.z + z;

                    BlockValue block = world.GetBlock(checkPos);

                    if (block.type == CaveBlocks.caveAir.type)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

}