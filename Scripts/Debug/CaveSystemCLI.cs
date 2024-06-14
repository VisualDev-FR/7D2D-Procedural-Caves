using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;

public class ConsoleCmdCaves : ConsoleCmdAbstract
{

    protected override string[] getCommands()
    {
        return new string[1] { "caves" };
    }

    protected override string getDescription()
    {
        return "Cave system commands";
    }

    private void StdOut(string message)
    {
        Log.Out(message);
    }

    private void ExecuteGetConfig()
    {
        List<string> outputs = new List<string>{
            $"---------------------------------------------------",
            $"CaveConfig",
            $"---------------------------------------------------",
            $"loggingEnabled {CaveConfig.loggingEnabled}",
            $"invert {CaveConfig.invert}",
            $"isSolid {CaveConfig.isSolid}",
            $"fractalType {CaveConfig.fractalType}",
            $"noiseType {CaveConfig.noiseType}",
            $"caveHeight2D {CaveConfig.caveHeight2D}",
            $"cavePos2D {CaveConfig.cavePos2D}",
            $"NoiseThreeshold {CaveConfig.NoiseThreeshold}",
            $"Octaves {CaveConfig.Octaves}",
            $"Lacunarity {CaveConfig.Lacunarity}",
            $"Gain {CaveConfig.Gain}",
            $"Frequency {CaveConfig.Frequency}",
            $"---------------------------------------------------",
        };

        Log.Out(string.Join("\n", outputs));
    }

    private void ExecuteSetConfig(List<string> _params)
    {
        if (_params.Count < 3)
        {
            StdOut("Missing arguments.");
            return;
        }

        string paramName = _params[1];
        string paramValue = _params[2];

        switch (paramName.ToLower())
        {
            case "frequency":
            case "freq":
                CaveConfig.Frequency = float.Parse(paramValue);
                break;

            case "threeshold":
            case "th":
                CaveConfig.NoiseThreeshold = float.Parse(paramValue);
                break;

            case "invert":
            case "inv":
                CaveConfig.invert = !CaveConfig.invert;
                break;

            case "pos":
                CaveConfig.cavePos2D = int.Parse(paramValue);
                break;

            case "height":
                CaveConfig.caveHeight2D = int.Parse(paramName);
                break;

            case "solid":
                CaveConfig.isSolid = !CaveConfig.isSolid;
                break;

            default:
                StdOut($"Invalid param name '{paramName}'");
                break;
        }
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        if (_params.Count == 0)
        {
            StdOut("No sub command given.");
            return;
        }

        switch (_params[0].ToLower())
        {
            case "getconfig":
                ExecuteGetConfig();
                break;

            case "setconfig":
                ExecuteSetConfig(_params);
                new ConsoleCmdRegionReset().Execute(new List<string>(), _senderInfo);
                break;

            default:
                StdOut("");
                break;
        }
    }
}