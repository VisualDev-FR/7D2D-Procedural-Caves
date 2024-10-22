using System.Collections.Generic;

public class DebugItemConsoleCmd : ConsoleCmdAbstract
{
    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        var player = GameManager.Instance.World.GetPrimaryPlayer();
        var itemValue = player.inventory.holdingItemItemValue;

        if (itemValue == null)
        {
            Log.Warning($"[DebugItem] player is not holding an item");
            return;
        }

        Log.Out($"[DebugItem] name: {itemValue.ItemClass.Name}");
        Log.Out($"[DebugItem] useTimes: {itemValue.UseTimes}");
        Log.Out($"[DebugItem] maxUseTimes: {itemValue.MaxUseTimes}");
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
