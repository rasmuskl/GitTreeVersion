using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion.Deployables
{
    public interface IVersionApplier
    {
        void ApplyVersion(AbsoluteFilePath filePath, SemVersion version);
    }
}