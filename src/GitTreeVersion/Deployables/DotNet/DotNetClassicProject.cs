using System;
using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion.Deployables.DotNet
{
    public class DotNetClassicProject : IDeployable
    {
        public DotNetClassicProject(AbsoluteFilePath filePath, AbsoluteFilePath[] referencedDeployablePaths)
        {
            FilePath = filePath;
            ReferencedDeployablePaths = referencedDeployablePaths;
        }

        public AbsoluteFilePath FilePath { get; }
        public AbsoluteFilePath[] ReferencedDeployablePaths { get; }

        public void ApplyVersion(SemVersion version)
        {
            throw new NotImplementedException();
        }
    }
}