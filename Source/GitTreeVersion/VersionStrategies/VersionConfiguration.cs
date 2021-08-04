namespace GitTreeVersion
{
    public class VersionConfiguration
    {
        public IVersionStrategy Major => new MajorFileBumpVersionStrategy();
        public IVersionStrategy Minor => new MinorFileBumpVersionStrategy();
        public IVersionStrategy Patch => new CommitCountVersionStrategy();
    }
}