using System;
using System.Linq;
using GitTreeVersion.BuildEnvironments;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Context
{
    public static class ContextResolver
    {
        public const string VersionConfigFileName = "version.json";

        public static VersionGraph GetVersionGraph(AbsoluteDirectoryPath startingPath, BuildEnvironmentDetector? buildEnvironmentDetector = null)
        {
            return GetVersionGraph(new[] { startingPath }, buildEnvironmentDetector);
        }

        public static VersionGraph GetVersionGraph(AbsoluteDirectoryPath[] startingPaths, BuildEnvironmentDetector? buildEnvironmentDetector = null)
        {
            if (!startingPaths.Any())
            {
                throw new Exception("No starting paths provided");
            }

            var repositoryRoot = AbsoluteDirectoryPath.FindDirectoryAboveContaining(startingPaths.First(), ".git");

            if (repositoryRoot is null)
            {
                throw new UserException("Not in a git repository.");
            }

            return new VersionGraph(repositoryRoot, startingPaths, buildEnvironmentDetector);
        }
    }
}