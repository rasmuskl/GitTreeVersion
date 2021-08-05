using System.Linq;
using GitTreeVersion.Paths;

namespace GitTreeVersion.VersionStrategies
{
    public class MinorFileBumpVersionStrategy : IVersionStrategy
    {
        public VersionComponent GetVersionComponent(AbsoluteDirectoryPath versionRootPath,
            AbsoluteDirectoryPath[] relevantPaths, string? range)
        {
            var minor = 0;

            if (range is null)
            {
                var minorVersionFiles = Git.GitFindFiles(versionRootPath, new[] { ":(glob).version/minor/*" });

                foreach (var file in minorVersionFiles)
                {
                    Log.Debug($"Minor version file: {file}");
                }

                if (minorVersionFiles.Any())
                {
                    minor = minorVersionFiles.Length;
                    var minorVersionCommits = Git.GitCommits(versionRootPath, null, new[] { ":(glob).version/minor/*" },
                        diffFilter: "A");

                    foreach (var commit in minorVersionCommits)
                    {
                        Log.Debug($"Minor version commit: {commit}");
                    }

                    if (minorVersionCommits.Any())
                    {
                        range = $"{minorVersionCommits.First()}..";
                    }
                }
            }
            else
            {
                var changedMinorFiles = Git.GitDiffFileNames(versionRootPath, range, ":(glob).version/minor/*");

                foreach (var file in changedMinorFiles)
                {
                    Log.Debug($"Minor version file: {file}");
                }

                minor = changedMinorFiles.Length;

                var minorVersionCommits =
                    Git.GitCommits(versionRootPath, range, changedMinorFiles.ToArray(), diffFilter: "A");

                foreach (var commit in minorVersionCommits)
                {
                    Log.Debug($"Minor version commit: {commit}");
                }

                if (minorVersionCommits.Any())
                {
                    range = $"{minorVersionCommits.First()}..";
                }
            }

            return new VersionComponent(minor, range);
        }
    }
}