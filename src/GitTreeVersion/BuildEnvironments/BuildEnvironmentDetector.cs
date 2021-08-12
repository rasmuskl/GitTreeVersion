using GitTreeVersion.BuildEnvironments.AzureDevOps;
using Spectre.Console;

namespace GitTreeVersion.BuildEnvironments
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
}