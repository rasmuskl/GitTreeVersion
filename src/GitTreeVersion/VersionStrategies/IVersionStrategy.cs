namespace GitTreeVersion.VersionStrategies
{
    public interface IVersionStrategy
    {
        VersionComponent GetVersionComponent(VersionComponentContext context, string? range);
    }
}