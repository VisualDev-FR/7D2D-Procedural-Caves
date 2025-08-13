using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

public class WorldGenConsoleCmd : ConsoleCmdAbstract
{
    private readonly DynamicPrefabDecorator dynamicPrefabDecorator = new DynamicPrefabDecorator();

    public List<PrefabInstance> AllPrefabs => dynamicPrefabDecorator.allPrefabs;

    public override bool AllowedInMainMenu => true;

    public override string[] getCommands()
    {
        return new string[] { "worldgen" };
    }

    public override string getDescription()
    {
        return "";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        var worldName = string.Join(" ", _params);

        if (worldName == "")
        {
            Log.Error("World name is required.");
            return;
        }

        GameManager.Instance.StartCoroutine(GenerateWorld(worldName));
    }

    public IEnumerator GenerateWorld(string worldName)
    {
        var caveBuilder = new CaveBuilder();
        var worldDatas = new WorldDatas(worldName);

        worldDatas.Debug();

        yield return caveBuilder.GenerateCaveFromWorld(worldDatas);
        yield return null;

        Logging.Debug($"loaded prefabs: {AllPrefabs.Count}");
    }



}