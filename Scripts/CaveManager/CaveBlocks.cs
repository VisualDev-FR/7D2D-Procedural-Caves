# pragma warning disable IDE1006

using System.IO;

public class CaveBlocks
{
    // vanilla blocks
    public static BlockValue concreteBlock => GetBlockValue("concreteShapes:cube");

    public static BlockValue climbableRopeBlock => GetBlockValue("modularRopeTiledSideCentered");

    // block placeholders
    public static BlockValue cntCaveFloor => GetBlockValue("cntCaveFloor");

    public static BlockValue cntCaveFloorFlat => GetBlockValue("cntCaveFloorFlat");

    public static BlockValue cntCaveCeiling => GetBlockValue("cntCaveCeiling");

    public static BlockValue cntCaveFlatWater => GetBlockValue("cntCaveFlatWater");

    public static BlockValue cntCaveFloorWater => GetBlockValue("cntCaveFloorWater");

    // cave blocks
    public static BlockValue caveAir => GetBlockValue("caveAir");

    public static BlockValue caveTerrStone => GetBlockValue("caveTerrStone");

    public static BlockValue caveTerrSnow => GetBlockValue("caveTerrSnow");

    public static BlockValue caveTerrSandStone => GetBlockValue("caveTerrSandStone");

    public static BlockValue caveTerrGravel => GetBlockValue("caveTerrGravel");

    public static BlockValue caveTerrOreIron => GetBlockValue("caveTerrOreIron");

    public static BlockValue caveTerrOreLead => GetBlockValue("caveTerrOreLead");

    public static BlockValue caveTerrOreCoal => GetBlockValue("caveTerrOreCoal");

    public static BlockValue caveTerrOrePotassiumNitrate => GetBlockValue("caveTerrOrePotassiumNitrate");

    public static BlockValue caveTerrOreOilDeposit => GetBlockValue("caveTerrOreOilDeposit");

    public static BlockValue GetBlockValue(string blockName)
    {
        if (Block.nameToBlock.TryGetValue(blockName, out var block))
        {
            return block.ToBlockValue();
        }

        throw new InvalidDataException($"block '{blockName}' does not exist. (case maybe invalid)");
    }

    public static bool IsTerrain(BlockValue blockValue)
    {
        return blockValue.Block.shape.IsTerrain();
    }

}