using System.Collections.Generic;

public abstract class CmdAbstract
{
    public abstract string[] GetCommands();

    public abstract void Execute(List<string> args);
}