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
        };

        public VersionPreset Preset { get; set; }
        public string? BaseVersion { get; set; }

        public string[]? ExtraDirectories { get; set; }

        public static VersionConfig Load(AbsoluteDirectoryPath versionRootPath)
        {
            var filePath = versionRootPath.CombineToFile(ContextResolver.VersionConfigFileName);
            VersionConfig? versionConfig = null;

            if (filePath.Exists)
            {
                var json = File.ReadAllText(filePath.ToString());
                versionConfig = FromJson(json);
            }

            return versionConfig ?? Default;
        }

        public static VersionConfig? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<VersionConfig>(json, JsonOptions.DefaultOptions);
        }
    }

    public enum VersionPreset
    {
        SemanticVersion,
        CalendarVersion,
        SemanticVersionFileBased,
    }
}