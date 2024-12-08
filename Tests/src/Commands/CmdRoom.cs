using System;
using System.Collections.Generic;
using System.Linq;

public class CmdRoom : CmdAbstract
{
    public override string[] GetCommands()
    {
        return new string[] { "room" };
    }

    public override void Execute(List<string> args)
    {
        var _ = SphereManager.spheres.Count;

        var timer = CaveUtils.StartTimer();
        var seed = DateTime.Now.GetHashCode();
        var random = new Random(seed);
        var size = new Vector3i(50, 20, 50);
        var prefab = new CavePrefab(0)
        {
            Size = size,
            position = new Vector3i(0, 20, 0),
        };
        prefab.UpdateMarkers(random);
        var room = new CaveRoom(prefab, seed);

        var voxels = room.GetBlocks().Select(pos => new Voxell(pos)).ToHashSet();

        Logging.Info($"timer: {timer.ElapsedMilliseconds}ms");

        // var voxels = new HashSet<Voxell>
        // {
        //     new Voxell(prefab.position, prefab.Size, WaveFrontMaterial.DarkGreen)
        // };

        // voxels.UnionWith(prefab.caveMarkers.Select(marker => new Voxell(position + marker.start, marker.size, WaveFrontMaterial.Orange)));

        DrawingUtils.GenerateObjFile("ignore/room.obj", voxels, false);
    }

}