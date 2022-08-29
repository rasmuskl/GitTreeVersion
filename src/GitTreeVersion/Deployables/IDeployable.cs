using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion.Deployables
{
    public interface IDeployable
    {
        AbsoluteFilePath FilePath { get; }
        AbsoluteFilePath[] ReferencedDeployablePaths { get; }
        void ApplyVersion(SemVersion version, ApplyOptions applyOptions);
    }
}