public class RequirementIsBelowTerrain : TargetedCompareRequirementBase
{
    public override bool IsValid(MinEventParams _params)
    {
        if (!ParamsValid(_params))
        {
            return false;
        }

        var player = _params.Self;

        return CaveGenerator.caveChunksProvider.IsCave(
            (int)player.position.x,
            (int)player.position.y,
            (int)player.position.z
        );
    }
}