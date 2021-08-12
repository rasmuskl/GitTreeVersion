namespace GitTreeVersion
{
    public class VersionConfig
    {
        public static readonly VersionConfig Default = new()
        {
            Mode = VersionMode.SemanticVersion,
        };

        public VersionMode Mode { get; set; }
        public string? Version { get; set; }
    }

    public enum VersionMode
    {
        SemanticVersion,
        CalendarVersion,
        SemanticVersionFileBased,
    }
}