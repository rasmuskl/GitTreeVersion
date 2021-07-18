using System;

namespace GitTreeVersion.Context
{
    public class Versionable
    {
        public Versionable(string path, VersionRoot versionRoot, Version version)
        {
            Path = path;
            VersionRoot = versionRoot;
            Version = version;
            DirectoryPath = System.IO.Path.GetDirectoryName(path)!;
        }

        public string Path { get; }
        public VersionRoot VersionRoot { get; }
        public Version Version { get; }
        public string DirectoryPath { get; }
    }
}