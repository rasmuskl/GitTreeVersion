namespace GitTreeVersion.BuildEnvironments
{
    public interface IEnvironmentAccessor
    {
        string? GetEnvironmentVariable(string variable);
    }
}