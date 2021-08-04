using System.Linq;
using GitTreeVersion.Paths;

namespace GitTreeVersion
{
    public class MajorFileBumpVersionStrategy : IVersionStrategy
    {
        public VersionComponent GetVersionComponent(AbsoluteDirectoryPath versionRootPath, AbsoluteDirectoryPath[] relevantPaths, string? range)
        {
            var majorVersionFiles = Git.GitFindFiles(versionRootPath, new[] {":(glob).version/major/*"});

            foreach (var file in majorVersionFiles)
            {
                Log.Debug($"Major version file: {file}");
            }

            if (majorVersionFiles.Any())
            {
                string[] majorVersionCommits = Git.GitCommits(versionRootPath, null, new[] {":(glob).version/major/*"}, diffFilter: "A");

                foreach (var majorVersionCommit in majorVersionCommits)
                {
                    Log.Debug($"Major version commit: {majorVersionCommit}");
                }

                range = $"{majorVersionCommits.First()}..";
            }

            return new VersionComponent(majorVersionFiles.Length, range);
        }
    }
}