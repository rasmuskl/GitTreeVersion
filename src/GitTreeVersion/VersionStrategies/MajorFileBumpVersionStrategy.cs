using System;
using System.IO;
using System.Linq;
using GitTreeVersion.Context;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;

namespace GitTreeVersion.VersionStrategies
{
    public class MajorFileBumpVersionStrategy : IVersionStrategy
    {
        public VersionComponent GetVersionComponent(VersionComponentContext context, string? range)
        {
            var gitDirectory = new GitDirectory(context.VersionRootPath);

            var majorVersionFiles = gitDirectory.GitFindFiles(new[] { ":(glob)**/.version/major/*" });

            foreach (var file in majorVersionFiles)
            {
                Log.Debug($"Major version file: {file}");
            }

            if (majorVersionFiles.Any())
            {
                string[] majorVersionCommits = gitDirectory.GitCommits(null, new[] { ":(glob)**/.version/major/*" }, diffFilter: "A");

                foreach (var majorVersionCommit in majorVersionCommits)
                {
                    Log.Debug($"Major version commit: {majorVersionCommit}");
                }

                range = $"{majorVersionCommits.First()}..";
            }

            return new VersionComponent(majorVersionFiles.Select(Path.GetFileName).Distinct().Count(), range);
        }

        public AbsoluteFilePath Bump(AbsoluteDirectoryPath versionRootPath)
        {
            var versionBumpDirectoryPath = versionRootPath.CombineToDirectory(".version", VersionType.Major.ToString().ToLowerInvariant());
            var versionBumpFilePath = versionBumpDirectoryPath.CombineToFile(DateTime.UtcNow.ToString("yyyyMMddHHmmssff"));
            Directory.CreateDirectory(versionBumpDirectoryPath.ToString());
            File.WriteAllText(versionBumpFilePath.ToString(), string.Empty);
            return versionBumpFilePath;
        }
    }
}