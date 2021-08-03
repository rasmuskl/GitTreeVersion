using System.Collections.Generic;
using GitTreeVersion.BuildEnvironments;
using GitTreeVersion.Context;

namespace GitTreeVersion.Tests
{
    public class DictionaryEnvironmentAccessor : IEnvironmentAccessor
    {
        private readonly Dictionary<string, string?> _environment;

        public DictionaryEnvironmentAccessor(Dictionary<string, string?> environment)
        {
            _environment = environment;
        }

        public string? GetEnvironmentVariable(string variable)
        {
            _environment.TryGetValue(variable, out var value);
            return value;
        }
    }
}