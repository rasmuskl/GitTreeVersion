using System;

namespace GitTreeVersion.BuildEnvironments
{
    public class DefaultEnvironmentAccessor : IEnvironmentAccessor
    {
        public string? GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }
    }
}