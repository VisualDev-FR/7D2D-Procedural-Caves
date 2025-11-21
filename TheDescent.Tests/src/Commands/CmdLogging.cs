using System.Collections.Generic;


public class CmdLogging : CmdAbstract
{

    public override string[] GetCommands()
    {
        return new string[] { "logging" };
    }

    public override void Execute(List<string> args)
    {
        Log.Out("Out");
        Log.Warning("Warning");
        Log.Error("Error");

        Logging.Debug("Debug ?");
        Logging.Info("Info :)");
        Logging.Warning("Warning !");
        Logging.Error("Error :(");


        var logger = Logging.CreateLogger("logger");

        // logger.Info("Debug ?");
        // logger.Info("Info :)");
        // logger.Warning("Warning !");
        // logger.Error("Error :()");
    }

}