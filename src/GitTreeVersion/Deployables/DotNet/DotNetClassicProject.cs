using System.IO;
using System.Reflection;
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
            var assemblyInfoFile = FilePath.Parent.CombineToFile("Properties", "AssemblyInfo.cs");

            if (!assemblyInfoFile.Exists)
            {
                Log.Warning("AssemblyInfo not found for project: " + FilePath.FullName);
                return;
            }

            var assemblyInfoContents = File.ReadAllText(assemblyInfoFile.FullName);

            var attributeAugmenter = new AttributeAugmenter();

            var dotnetCompatibleVersion = version.Change(prerelease: null, build: null);
            assemblyInfoContents = attributeAugmenter.EnsureStringAttributes(assemblyInfoContents, new[]
            {
                new StringAttribute(typeof(AssemblyInformationalVersionAttribute), version.ToString()),
                new StringAttribute(typeof(AssemblyVersionAttribute), dotnetCompatibleVersion.ToString()),
                new StringAttribute(typeof(AssemblyFileVersionAttribute), dotnetCompatibleVersion.ToString()),
            });

            File.Copy(assemblyInfoFile.FullName, $"{assemblyInfoFile.FullName}.bak", true);
            File.WriteAllText(assemblyInfoFile.FullName, assemblyInfoContents);
        }
    }
}