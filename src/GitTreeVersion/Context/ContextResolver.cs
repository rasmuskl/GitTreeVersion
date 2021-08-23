using GitTreeVersion.BuildEnvironments;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Context
{
    public static class ContextResolver
    {
        public const string VersionConfigFileName = "version.json";

        public static VersionGraph GetVersionGraph(AbsoluteDirectoryPath startingPath, BuildEnvironmentDetector? buildEnvironmentDetector = null)
        {
            var repositoryRoot = AbsoluteDirectoryPath.FindDirectoryAboveContaining(startingPath, ".git");

            if (repositoryRoot is null)
            {
                throw new UserException("Not in a git repository.");
            }

            return new VersionGraph(repositoryRoot, startingPath, buildEnvironmentDetector);
        }
    }
}