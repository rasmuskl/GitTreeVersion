using System.Linq;
using GitTreeVersion.Git;

namespace GitTreeVersion.VersionStrategies
{
    public class MinorFileBumpVersionStrategy : IVersionStrategy
    {
        public VersionComponent GetVersionComponent(VersionComponentContext context, string? range)
        {
            var gitDirectory = new GitDirectory(context.VersionRootPath);
            var minor = 0;

            if (range is null)
            {
                var minorVersionFiles = gitDirectory.GitFindFiles(new[] { ":(glob).version/minor/*" });

                foreach (var file in minorVersionFiles)
                {
                    Log.Debug($"Minor version file: {file}");
                }

                if (minorVersionFiles.Any())
                {
                    minor = minorVersionFiles.Length;
                    var minorVersionCommits = gitDirectory.GitCommits(null, new[] { ":(glob).version/minor/*" },
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
                var changedMinorFiles = gitDirectory.GitDiffFileNames(range, ":(glob).version/minor/*");

                foreach (var file in changedMinorFiles)
                {
                    Log.Debug($"Minor version file: {file}");
                }

                minor = changedMinorFiles.Length;

                var minorVersionCommits = gitDirectory.GitCommits(range, changedMinorFiles.ToArray(), diffFilter: "A");

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