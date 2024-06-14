public static class AdvLogging
{
    public static void DisplayLog(string AdvFeatureClass, string strDisplay)
    {
        if (!CaveConfig.CheckFeatureStatus(AdvFeatureClass, "Logging"))
            return;

        if (CaveConfig.CheckFeatureStatus("AdvancedLogging", "LowOutput"))
            Log.Out($"{strDisplay}");
        else
        {
            //Debug.Log(strDisplay);
            Log.Out($"{AdvFeatureClass} :: {strDisplay}");
        }

    }


    public static void DisplayLog(string AdvFeatureClass, string Feature, string strDisplay)
    {

        if (!CaveConfig.CheckFeatureStatus(AdvFeatureClass, Feature))
            return;

        if (CaveConfig.CheckFeatureStatus("AdvancedLogging", "LowOutput"))
            Log.Out($"{strDisplay}");
        else
            Log.Out($"{AdvFeatureClass} :: {Feature} :: {strDisplay}");
    }

    public static bool LogEnabled(string AdvFeatureClass)
    {
        return CaveConfig.CheckFeatureStatus(AdvFeatureClass);
    }

    public static bool LogEnabled(string AdvFeatureClass, string Feature)
    {
        return CaveConfig.CheckFeatureStatus(AdvFeatureClass, Feature);
    }
}