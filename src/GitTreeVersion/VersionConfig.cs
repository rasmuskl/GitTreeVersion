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
    }

    public enum VersionPreset
    {
        SemanticVersion,
        CalendarVersion,
        SemanticVersionFileBased,
    }
}