using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Commands
{
    public class CheckChangedCommand : Command
    {
        public CheckChangedCommand() : base("check-changed", "Checks if relevant files have changed")
        {
            Handler = CommandHandler.Create(Execute);
        }

        private void Execute()
        {
            var output = Git.RunGit(new AbsoluteDirectoryPath(Environment.CurrentDirectory), "rev-list", "--parents", "--max-count=1", "HEAD").Trim();

            var commitShas = output.Split(" ");

            if (commitShas.Length != 3)
            {
                throw new InvalidOperationException("Last commit is not a merge commit.");
            }

            var parent1 = commitShas[1];
            var parent2 = commitShas[2];
            
            Console.WriteLine($"Found merge commit with parents {parent1} and {parent2}.");
            
            Console.WriteLine();
            Console.WriteLine("Relevant changed files:");
            Console.WriteLine();

            Console.WriteLine(Git.RunGit(new AbsoluteDirectoryPath(Environment.CurrentDirectory), "diff", "--name-only", parent1, parent2).Trim());
        }
    }
}