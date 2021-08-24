using System.IO;
using System.Text.Json;
using GitTreeVersion.Context;
using GitTreeVersion.Paths;

namespace GitTreeVersion
{
    public class VersionConfig
    {
        public static readonly VersionConfig Default = new()
        {
            Preset = VersionPreset.SemanticVersion,
            BaseVersion = "0.0.0",
        };

        public VersionPreset Preset { get; set; }
        public string? BaseVersion { get; set; }

        public static VersionConfig Load(AbsoluteDirectoryPath versionRootPath)
        {
            var filePath = versionRootPath.CombineToFile(ContextResolver.VersionConfigFileName);
            VersionConfig? versionConfig = null;

            if (filePath.Exists)
            {
                versionConfig = JsonSerializer.Deserialize<VersionConfig>(File.ReadAllText(filePath.ToString()), JsonOptions.DefaultOptions);
            }

            return versionConfig ?? Default;
        }
    }

    public enum VersionPreset
    {
        SemanticVersion,
        CalendarVersion,
        SemanticVersionFileBased,
    }
}