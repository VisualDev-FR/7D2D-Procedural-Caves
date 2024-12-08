public class TestCaveBlock
{

    public static void Test_CaveBlock_HashZX()
    {
        int x = 64045;
        int z = 4687;

        var hash = CaveBlock.HashZX(x, z);
        CaveBlock.ZXFromHash(hash, out var x1, out var z1);

        Logging.Info($"{x}, {z}");
        Logging.Info($"{x1}, {z1}");

        CaveUtils.Assert(x == x1, $"x : {x1}, expected: {x}");
        CaveUtils.Assert(z == z1, $"z : {z1}, expected: {z}");
    }

}