namespace GitTreeVersion
{
    public class VersionConfig
    {
        public VersionMode Mode { get; set; }
        
        public static readonly VersionConfig Default = new()
        {
            Mode = VersionMode.SemanticVersion
        };
    }

    public enum VersionMode
    {
        SemanticVersion,
        CalendarVersion,
    }
}