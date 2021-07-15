using System.IO;
using System.Text.Json;

namespace GitTreeVersion
{
    public class VersionConfigManager
    {
        public const string VersionConfigFileName = "version.json";

        public VersionConfigInstance FindConfig(string workingDirectory)
        {
            var fileName = Path.Combine(workingDirectory, VersionConfigFileName);

            if (!File.Exists(fileName))
            {
                if (Directory.Exists(Path.Combine(workingDirectory, ".git")))
                {
                    return new VersionConfigInstance(workingDirectory, VersionConfig.Default);
                }

                var parentDirectory = Path.GetDirectoryName(workingDirectory);

                if (parentDirectory is null)
                {
                    return new VersionConfigInstance(workingDirectory, VersionConfig.Default);
                }

                return FindConfig(parentDirectory);
            }

            var content = File.ReadAllText(fileName);
            return new VersionConfigInstance(workingDirectory, JsonSerializer.Deserialize<VersionConfig>(content)!);
        }
    }

    public class VersionConfigInstance
    {
        public string Path { get; }
        public VersionConfig VersionConfig { get; }

        public VersionConfigInstance(string path, VersionConfig versionConfig)
        {
            Path = path;
            VersionConfig = versionConfig;
        }
    }
}