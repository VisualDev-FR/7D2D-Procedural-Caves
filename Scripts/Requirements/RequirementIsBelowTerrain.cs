public class RequirementIsBelowTerrain : TargetedCompareRequirementBase
{
    public override bool IsValid(MinEventParams _params)
    {
        if (!ParamsValid(_params))
        {
            return false;
        }

        var player = _params.Self;
        var playerPosY = player.position.y + CaveConfig.terrainMargin;
        var terrainHeight = GameManager.Instance.World.GetHeight((int)player.position.x, (int)player.position.z);
        var isBelowTerrain = playerPosY < terrainHeight;

        return invert ? !isBelowTerrain : isBelowTerrain;
    }
}