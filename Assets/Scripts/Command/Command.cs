using System.Collections.Generic;

// This class represents any command in a sequence.
public class Command
{
    public CommandType Type;

    // --- Properties specific to Loop commands ---
    public int RepeatCount = 1;
    public List<Command> SubCommands = new List<Command>();
}