using System;

namespace GitTreeVersion.Git
{
    public class GitFailedException : Exception
    {
        public GitFailedException(string[] arguments, string errorOutput, int exitCode) : base($"Git failed - exit code: {exitCode}. Command: git {string.Join(' ', arguments)} {errorOutput}")
        {
            Arguments = arguments;
            ErrorOutput = errorOutput.Trim();
            ExitCode = exitCode;
        }

        public string[] Arguments { get; }
        public string ErrorOutput { get; }
        public int ExitCode { get; }
    }
}