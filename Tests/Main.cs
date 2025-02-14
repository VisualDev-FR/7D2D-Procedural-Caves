using System.Linq;

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
    };

    public static void Main(string[] args)
    {
        commands.Where(cmd => cmd.GetCommands()
            .Contains(args[0]))
            .First()
            .Execute(args.ToList());
    }
}


