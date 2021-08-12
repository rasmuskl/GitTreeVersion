using System.Linq;
using GitTreeVersion.Git;

namespace GitTreeVersion.VersionStrategies
{
    public class MajorFileBumpVersionStrategy : IVersionStrategy
    {
        public VersionComponent GetVersionComponent(VersionComponentContext context, string? range)
        {
            var gitDirectory = new GitDirectory(context.VersionRootPath);

            var majorVersionFiles = gitDirectory.GitFindFiles(new[] { ":(glob).version/major/*" });

            foreach (var file in majorVersionFiles)
            {
                Log.Debug($"Major version file: {file}");
            }

            if (majorVersionFiles.Any())
            {
                string[] majorVersionCommits = gitDirectory.GitCommits(null, new[] { ":(glob).version/major/*" }, diffFilter: "A");

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