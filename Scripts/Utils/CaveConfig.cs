using System;

public static class CaveConfig
{
    public static FastNoiseLite fastNoise;

    public static FastNoiseLite.FractalType fractalType;

    public static FastNoiseLite.NoiseType noiseType;

    public static int seed;

    public static int Octaves;

    public static float Lacunarity;

    public static float Gain;

    public static float Frequency;

    public static float NoiseThreeshold = float.Parse(GetPropertyValue("CaveConfiguration", "CaveThreshold"));

    public static bool loggingEnabled;

    public static bool invert = bool.Parse(GetPropertyValue("CaveConfiguration", "invert"));

    public static int caveHeight2D = 5;

    public static int cavePos2D = 5;

    public static bool isSolid = false;

    public static FastNoiseLite.FractalType ParseFractalType(string typeName)
    {
        switch (typeName)
        {
            case "DomainWarpIndependent":
                return FastNoiseLite.FractalType.DomainWarpIndependent;

            case "DomainWarpProgressive":
                return FastNoiseLite.FractalType.DomainWarpProgressive;

            case "FBm":
                return FastNoiseLite.FractalType.FBm;

            case "PingPong":
                return FastNoiseLite.FractalType.PingPong;

            case "Ridged":
                return FastNoiseLite.FractalType.Ridged;

            default:
                break;
        }
        return FastNoiseLite.FractalType.None;
    }

    public static FastNoiseLite.NoiseType ParseNoiseType(string noiseName)
    {
        switch (noiseName)
        {
            case "Cellular":
                return FastNoiseLite.NoiseType.Cellular;

            case "OpenSimplex2":
                return FastNoiseLite.NoiseType.OpenSimplex2;

            case "OpenSimplex2S":
                return FastNoiseLite.NoiseType.OpenSimplex2S;

            case "Perlin":
                return FastNoiseLite.NoiseType.Perlin;

            case "Value":
                return FastNoiseLite.NoiseType.Value;

            case "ValueCubic":
                return FastNoiseLite.NoiseType.ValueCubic;

            default:
                break;
        }

        throw new Exception($"Invalid Noise type: '{noiseName}'");
    }

    private static FastNoiseLite InitFastNoise()
    {
        var AdvFeatureClass = "CaveConfiguration";

        fastNoise = new FastNoiseLite();

        fractalType = ParseFractalType(CaveConfig.GetPropertyValue(AdvFeatureClass, "FractalType"));
        noiseType = ParseNoiseType(CaveConfig.GetPropertyValue(AdvFeatureClass, "NoiseType"));

        Octaves = int.Parse(CaveConfig.GetPropertyValue(AdvFeatureClass, "Octaves"));
        Lacunarity = float.Parse(CaveConfig.GetPropertyValue(AdvFeatureClass, "Lacunarity"));
        Gain = float.Parse(CaveConfig.GetPropertyValue(AdvFeatureClass, "Gain"));
        Frequency = float.Parse(CaveConfig.GetPropertyValue(AdvFeatureClass, "Frequency"));

        return fastNoise;
    }

    public static FastNoiseLite GetFastNoise(Chunk chunk)
    {

        if (fastNoise == null) fastNoise = InitFastNoise();

        fastNoise.SetSeed(seed);
        fastNoise.SetFractalType(fractalType);
        fastNoise.SetNoiseType(noiseType);
        fastNoise.SetFractalOctaves(Octaves);
        fastNoise.SetFractalLacunarity(Lacunarity);
        fastNoise.SetFractalGain(Gain);
        fastNoise.SetFrequency(Frequency);

        return fastNoise;
    }

    public static bool CheckFeatureStatus(string strFeature)
    {
        var ConfigurationFeatureBlock = Block.GetBlockValue("ConfigFeatureBlock");
        if (ConfigurationFeatureBlock.type == 0)
            return false;

        var result = false;
        if (ConfigurationFeatureBlock.Block.Properties.Contains(strFeature))
            result = ConfigurationFeatureBlock.Block.Properties.GetBool(strFeature);

        return result;
    }

    public static bool RequiredModletAvailable(string strClass)
    {
        // Check if the feature has a required modlet defined.
        var requiredModlet = CaveConfig.GetPropertyValue(strClass, "RequiredModlet");

        // None? pass through the results.
        if (string.IsNullOrEmpty(requiredModlet)) return true;

        var requiredModlets = requiredModlet.Split(',');
        foreach (var requiredMod in requiredModlet.Split(','))
        {
            if (!ModManager.ModLoaded(requiredMod))
            {
                Log.Out($"WARN: RequiredModlet is defined on {strClass}: {requiredMod} not found. Feature is turned off.");
                return false;
            }
        }

        return true;
    }

    public static bool CheckFeatureStatus(string strClass, string strFeature)
    {
        var ConfigurationFeatureBlock = Block.GetBlockValue("ConfigFeatureBlock");
        if (ConfigurationFeatureBlock.type == 0)
            return false;


        var result = false;
        if (ConfigurationFeatureBlock.Block.Properties.Classes.ContainsKey(strClass))
        {
            var dynamicProperties3 = ConfigurationFeatureBlock.Block.Properties.Classes[strClass];
            foreach (var keyValuePair in dynamicProperties3.Values.Dict.Dict)
                if (string.Equals(keyValuePair.Key, strFeature, StringComparison.CurrentCultureIgnoreCase))
                    result = StringParsers.ParseBool(dynamicProperties3.Values[keyValuePair.Key]);
        }

        if (result)
            result = RequiredModletAvailable(strClass);


        return result;
    }

    public static string GetPropertyValue(string strClass, string strFeature)
    {
        var ConfigurationFeatureBlock = Block.GetBlockValue("ConfigFeatureBlock");
        if (ConfigurationFeatureBlock.type == 0)
            return string.Empty;


        var result = string.Empty;
        if (ConfigurationFeatureBlock.Block.Properties.Classes.ContainsKey(strClass))
        {
            var dynamicProperties3 = ConfigurationFeatureBlock.Block.Properties.Classes[strClass];
            foreach (var keyValuePair in dynamicProperties3.Values.Dict.Dict)
                if (keyValuePair.Key == strFeature)
                    return keyValuePair.Value.ToString();
        }

        return result;
    }
}