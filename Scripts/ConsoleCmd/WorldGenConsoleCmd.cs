
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WorldGenConsoleCmd : ConsoleCmdAbstract
{
    private readonly DynamicPrefabDecorator dynamicPrefabDecorator = new DynamicPrefabDecorator();

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
            worldName = "Old Honihebu County";
        }

        GameManager.Instance.StartCoroutine(GenerateWorld(worldName));
    }

    public IEnumerator GenerateWorld(string worldName)
    {
        var caveBuilder = new CaveBuilder();
        var worldDatas = new WorldDatas(worldName);

        worldDatas.Debug();

        yield return caveBuilder.GenerateCaveFromWorld(worldDatas);
    }

}

