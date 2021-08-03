using System;
using System.IO;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Context
{
    public class Bumper
    {
        public AbsoluteFilePath Bump(AbsoluteDirectoryPath versionRootPath, VersionType versionType)
        {
            var versionBumpDirectoryPath = Path.Combine(versionRootPath.ToString(), ".version", versionType.ToString().ToLowerInvariant());
            var versionBumpFilePath = Path.Combine(versionBumpDirectoryPath, DateTime.UtcNow.ToString("yyyyMMddHHmmssff"));
            Directory.CreateDirectory(versionBumpDirectoryPath);
            File.WriteAllText(versionBumpFilePath, string.Empty);
            return new AbsoluteFilePath(versionBumpFilePath);
        }
    }

    public enum VersionType
    {
        Major,
        Minor
    }
}