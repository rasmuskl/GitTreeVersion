using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GitTreeVersion.Context
{
    public class VersionRoot
    {
        public VersionRoot(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public ImmutableList<VersionRoot> VersionRoots { get; private set; } = ImmutableList<VersionRoot>.Empty;

        public void AddVersionRoot(VersionRoot versionRoot)
        {
            VersionRoots = VersionRoots.Add(versionRoot);
        }

        public IEnumerable<VersionRoot> AllVersionRoots()
        {
            yield return this;

            foreach (var versionRoot in VersionRoots.SelectMany(r => r.AllVersionRoots()))
            {
                yield return versionRoot;
            }
        }
    }
}