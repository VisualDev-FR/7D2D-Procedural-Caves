using System.Collections.Generic;

public class DebugItemConsoleCmd : ConsoleCmdAbstract
{
    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        var player = GameManager.Instance.World.GetPrimaryPlayer();
        var itemValue = player.inventory.holdingItemItemValue;
        var itemData = player.inventory.holdingItemData;
        var itemClass = player.inventory.holdingItem;

        foreach (var partName in player.parts.Keys)
        {
            Logging.Info($"[DebugItem] part: {partName}");
        }

        foreach (var mod in itemValue.Modifications)
        {
            Logging.Info($"[DebugItem] mod: {mod.ItemClass.Name}");
        }

        if (itemValue == null)
        {
            Logging.Warning($"[DebugItem] player is not holding an item");
            return;
        }

        Logging.Info($"[DebugItem] name: {itemValue.ItemClass.Name}");
        Logging.Info($"[DebugItem] Quality: {itemValue.Quality}");
        Logging.Info($"[DebugItem] useTimes: {itemValue.UseTimes}");
        Logging.Info($"[DebugItem] maxUseTimes: {itemValue.MaxUseTimes}");
    }

    public override string[] getCommands()
    {
        return new string[] { "debugitem", "di" };
    }

    public override string getDescription()
    {
        return "DebugItemConsoleCmd";
    }
}
