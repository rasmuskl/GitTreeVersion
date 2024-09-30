using System.CommandLine;

namespace GitTreeVersion.Commands.Deployable;

public class DeployableCommand : Command
{
    public DeployableCommand() : base("deployable", "Manage deployables")
    {
        AddCommand(new ListDeployablesCommand());
    }
}