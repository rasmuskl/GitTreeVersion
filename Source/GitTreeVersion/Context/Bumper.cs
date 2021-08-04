using System;
using System.IO;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Context
{
    public class Bumper
    {
        public AbsoluteFilePath Bump(AbsoluteDirectoryPath versionRootPath, VersionType versionType)
        {
            var versionBumpDirectoryPath = versionRootPath.CombineToDirectory(".version", versionType.ToString().ToLowerInvariant());
            var versionBumpFilePath = versionBumpDirectoryPath.CombineToFile(DateTime.UtcNow.ToString("yyyyMMddHHmmssff"));
            Directory.CreateDirectory(versionBumpDirectoryPath.ToString());
            File.WriteAllText(versionBumpFilePath.ToString(), string.Empty);
            return versionBumpFilePath;
        }
    }

    public enum VersionType
    {
        Major,
        Minor
    }
}
