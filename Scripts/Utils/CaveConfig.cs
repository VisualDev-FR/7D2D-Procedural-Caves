using System;

[Serializable]
public class NoiseConfig
{
    public FastNoiseLite.FractalType fractalType;

    public FastNoiseLite.NoiseType noiseType;

    public int octaves;

    public float lacunarity;

    public float gain;

    public float frequency;

    public void SetFractalType(string typeName)
    {
        switch (typeName)
        {
            case "DomainWarpIndependent":
                fractalType = FastNoiseLite.FractalType.DomainWarpIndependent;
                return;

            case "DomainWarpProgressive":
                fractalType = FastNoiseLite.FractalType.DomainWarpProgressive;
                return;

            case "FBm":
                fractalType = FastNoiseLite.FractalType.FBm;
                return;

            case "PingPong":
                fractalType = FastNoiseLite.FractalType.PingPong;
                return;

            case "Ridged":
                fractalType = FastNoiseLite.FractalType.Ridged;
                return;

            default:
                break;
        }

        throw new Exception($"Invalid Fractal type: '{typeName}'");
    }

    public void SetNoiseType(string noiseName)
    {
        switch (noiseName)
        {
            case "Cellular":
                noiseType = FastNoiseLite.NoiseType.Cellular;
                return;

            case "OpenSimplex2":
                noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
                return;

            case "OpenSimplex2S":
                noiseType = FastNoiseLite.NoiseType.OpenSimplex2S;
                return;

            case "Perlin":
                noiseType = FastNoiseLite.NoiseType.Perlin;
                return;

            case "Value":
                noiseType = FastNoiseLite.NoiseType.Value;
                return;

            case "ValueCubic":
                noiseType = FastNoiseLite.NoiseType.ValueCubic;
                return;

            default:
                break;
        }

        throw new Exception($"Invalid Noise type: '{noiseName}'");
    }

    public FastNoiseLite GetNoise(int seed)
    {
        var fastNoise = new FastNoiseLite(seed);

        fastNoise.SetFractalType(fractalType);
        fastNoise.SetNoiseType(noiseType);
        fastNoise.SetFractalOctaves(octaves);
        fastNoise.SetFractalLacunarity(lacunarity);
        fastNoise.SetFractalGain(gain);
        fastNoise.SetFrequency(frequency);

        return fastNoise;
    }
}


public static class CaveConfig
{
    public static NoiseConfig noiseZX;

    public static NoiseConfig noiseY;

    public static bool loggingEnabled;

    public static float NoiseThreeshold = float.Parse(GetPropertyValue("CaveConfiguration", "CaveThreshold"));

    public static bool invert = bool.Parse(GetPropertyValue("CaveConfiguration", "invert"));

    public static int caveHeight2D = 5;

    public static int cavePos2D = 5;

    public static bool isSolid = false;

    public static int seed = int.Parse(GetPropertyValue("CaveConfiguration", "seed"));


    private static NoiseConfig InitFastNoiseZX()
    {
        var AdvFeatureClass = "CaveConfiguration";

        noiseZX = new NoiseConfig()
        {
            octaves = int.Parse(GetPropertyValue(AdvFeatureClass, "OctavesZX")),
            gain = float.Parse(GetPropertyValue(AdvFeatureClass, "GainZX")),
            frequency = float.Parse(GetPropertyValue(AdvFeatureClass, "FrequencyZX")),
            lacunarity = float.Parse(GetPropertyValue(AdvFeatureClass, "LacunarityZX")),
        };

        noiseZX.SetFractalType(GetPropertyValue(AdvFeatureClass, "FractalTypeZX"));
        noiseZX.SetNoiseType(GetPropertyValue(AdvFeatureClass, "NoiseTypeZX"));

        return noiseZX;
    }

    private static NoiseConfig InitFastNoiseY()
    {
        var AdvFeatureClass = "CaveConfiguration";

        noiseY = new NoiseConfig()
        {
            octaves = int.Parse(GetPropertyValue(AdvFeatureClass, "OctavesY")),
            gain = float.Parse(GetPropertyValue(AdvFeatureClass, "GainY")),
            frequency = float.Parse(GetPropertyValue(AdvFeatureClass, "FrequencyY")),
            lacunarity = float.Parse(GetPropertyValue(AdvFeatureClass, "LacunarityY")),
        };

        noiseY.SetFractalType(GetPropertyValue(AdvFeatureClass, "FractalTypeY"));
        noiseY.SetNoiseType(GetPropertyValue(AdvFeatureClass, "NoiseTypeY"));

        return noiseY;
    }

    public static FastNoiseLite GetFastNoiseZX()
    {

        if (noiseZX == null)
            noiseZX = InitFastNoiseZX();

        return noiseZX.GetNoise(seed);
    }

    public static FastNoiseLite GetFastNoiseY()
    {

        if (noiseY == null)
            noiseY = InitFastNoiseY();

        return noiseY.GetNoise(seed);
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