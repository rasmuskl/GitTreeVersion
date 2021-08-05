using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion.VersionAppliers
{
    public interface IVersionApplier
    {
        void ApplyVersion(AbsoluteFilePath filePath, SemVersion version);
    }
}