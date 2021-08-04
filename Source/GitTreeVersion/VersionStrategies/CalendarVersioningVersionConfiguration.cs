namespace GitTreeVersion.VersionStrategies
{
    public class CalendarVersioningVersionConfiguration : IVersionConfiguration
    {
        public IVersionStrategy Major => new FullDateVersionStrategy();
        public IVersionStrategy Minor => new CommitCountVersionStrategy();
        public IVersionStrategy Patch => new FixedVersionStrategy(0);
    }
}
