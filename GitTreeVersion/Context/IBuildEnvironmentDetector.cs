using System;
using Spectre.Console;

namespace GitTreeVersion.Context
{
    public class BuildEnvironmentDetector
    {
        private readonly IEnvironmentAccessor _environmentAccessor;

        public BuildEnvironmentDetector(IEnvironmentAccessor? environmentAccessor = null)
        {
            _environmentAccessor = environmentAccessor ?? new DefaultEnvironmentAccessor();
        }

        public IBuildEnvironment? GetBuildEnvironment()
        {
            if (_environmentAccessor.GetEnvironmentVariable("TF_BUILD") == "True")
            {
                AnsiConsole.MarkupLine("Build Environment: [green]Azure Pipelines[/]");
                return new AzureDevOpsBuildEnvironment(_environmentAccessor);
            }

            return null;
        }
    }

    public interface IEnvironmentAccessor
    {
        string? GetEnvironmentVariable(string variable);
    }

    public class DefaultEnvironmentAccessor : IEnvironmentAccessor
    {
        public string? GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }
    }
}