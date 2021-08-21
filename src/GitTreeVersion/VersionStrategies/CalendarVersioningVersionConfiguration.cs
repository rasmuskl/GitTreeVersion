namespace GitTreeVersion.VersionStrategies
{
    public class CalendarVersioningVersionConfiguration : IVersionConfiguration
    {
        public IVersionStrategy Major => new CommitDayDateVersionStrategy("yyyy");
        public IVersionStrategy Minor => new CommitDayDateVersionStrategy("Mdd");
        public IVersionStrategy Patch => new CommitCountVersionStrategy();
    }
}