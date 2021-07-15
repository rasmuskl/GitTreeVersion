namespace GitTreeVersion
{
    public class VersionConfig
    {
        public string? Major { get; set; }
        public string? Minor { get; set; }
        public string? Patch { get; set; }
        public string? Revision { get; set; }
        
        public static readonly VersionConfig Default = new();
    }
}