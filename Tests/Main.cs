using System;
using System.Linq;
using System.Reflection;

public static class Program
{
    private static readonly CmdAbstract[] commands = new CmdAbstract[]
    {
        new CmdBezier(),
        new CmdCave(),
        new CmdClusterize(),
        new CmdGraph(),
        new CmdGraphDebug(),
        new CmdNoise(),
        new CmdNoise1D(),
        new CmdPath(),
        new CmdRegion(),
        new CmdRoom(),
        new CmdSphere(),
        new CmdTunnel(),
        new CmdLogging(),
        new CmdDelaunay(),
    };

    public static void Main(string[] args)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var harmony = new HarmonyLib.Harmony(assembly.GetName().ToString());
        harmony.PatchAll(assembly);

        var command = commands.Where(cmd => cmd.GetCommands()
            .Contains(args[0]))
            .FirstOrDefault();

        if (command != null)
        {
            command.Execute(args.ToList());
        }
        else
        {
            Console.WriteLine($"Can't find command '{args[0]}'");
        }
    }
}


