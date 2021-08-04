namespace GitTreeVersion.VersionStrategies
{
    public class SemanticVersioningVersionConfiguration : IVersionConfiguration
    {
        public IVersionStrategy Major => new MajorFileBumpVersionStrategy();
        public IVersionStrategy Minor => new MinorFileBumpVersionStrategy();
        public IVersionStrategy Patch => new CommitCountVersionStrategy();
    }
}
