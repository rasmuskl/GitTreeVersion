namespace GitTreeVersion.VersionStrategies
{
    public interface IVersionConfiguration
    {
        IVersionStrategy Major { get; }
        IVersionStrategy Minor { get; }
        IVersionStrategy Patch { get; }
    }
}