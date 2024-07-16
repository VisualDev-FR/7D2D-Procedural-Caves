using System.Collections.Generic;
using System;

public class ConsoleCmdCaves : ConsoleCmdAbstract
{

    public override string[] getCommands()
    {
        return new string[1] { "caves" };
    }

    public override string getDescription()
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
            $"caveHeight2D {CaveConfig.caveHeight2D}",
            $"cavePos2D {CaveConfig.cavePos2D}",
            $"NoiseThreeshold {CaveConfig.NoiseThreeshold}",
            $"seed {CaveConfig.seed}",
            $"--------------------------",
            $"ZX fractalType {CaveConfig.noiseZX.fractalType}",
            $"ZX noiseType {CaveConfig.noiseZX.noiseType}",
            $"ZX Octaves {CaveConfig.noiseZX.octaves}",
            $"ZX Lacunarity {CaveConfig.noiseZX.lacunarity}",
            $"ZX Gain {CaveConfig.noiseZX.gain}",
            $"ZX Frequency {CaveConfig.noiseZX.frequency}",
            $"--------------------------",
            $"Y fractalType {CaveConfig.noiseY.fractalType}",
            $"Y noiseType {CaveConfig.noiseY.noiseType}",
            $"Y Octaves {CaveConfig.noiseY.octaves}",
            $"Y Lacunarity {CaveConfig.noiseY.lacunarity}",
            $"Y Gain {CaveConfig.noiseY.gain}",
            $"Y Frequency {CaveConfig.noiseY.frequency}",
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
            case "seed":
                CaveConfig.seed = int.Parse(paramValue);
                break;

            case "zxfrequency":
            case "zxfreq":
                CaveConfig.noiseZX.frequency = float.Parse(paramValue);
                break;

            case "threeshold":
            case "th":
                CaveConfig.NoiseThreeshold = float.Parse(paramValue);
                break;

            case "invert":
            case "inv":
                CaveConfig.invert = !CaveConfig.invert;
                break;

            case "2dpos":
                CaveConfig.cavePos2D = int.Parse(paramValue);
                break;

            case "2dheight":
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
            XUiC_CavesConfig.Open();
            return;
        }

        switch (_params[0].ToLower())
        {
            case "get":
                ExecuteGetConfig();
                break;

            case "set":
                ExecuteSetConfig(_params);
                new ConsoleCmdRegionReset().Execute(new List<string>(), _senderInfo);
                break;

            default:
                break;
        }
    }

    public void ForceRegionReset()
    {
        World world = GameManager.Instance.World;

        ChunkCluster chunkCache = world.ChunkCache;
        ChunkProviderGenerateWorld chunkProviderGenerateWorld = chunkCache.ChunkProvider as ChunkProviderGenerateWorld;

        HashSetLong hashSetLong = chunkProviderGenerateWorld.ResetAllChunks(ChunkProtectionLevel.None);

        if (chunkProviderGenerateWorld == null)
        {
            Log.Error("Failed to reset regions: ChunkProviderGenerateWorld could not be found for current world instance.");
            return;
        }

        SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"Regenerating {hashSetLong.Count} synced chunks.");
        foreach (long item3 in hashSetLong)
        {
            chunkProviderGenerateWorld.GenerateSingleChunk(chunkCache, item3, _forceRebuild: true);
        }

        SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Regeneration complete.");
        SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Region reset complete.");
    }

}