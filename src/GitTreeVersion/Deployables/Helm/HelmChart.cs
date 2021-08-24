using System;
using System.IO;
using System.Linq;
using GitTreeVersion.Paths;
using Semver;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace GitTreeVersion.Deployables.Helm
{
    public class HelmChart : IDeployable
    {
        public HelmChart(AbsoluteFilePath filePath)
        {
            FilePath = filePath;
        }

        public AbsoluteFilePath FilePath { get; }
        public AbsoluteFilePath[] ReferencedDeployablePaths { get; } = Array.Empty<AbsoluteFilePath>();

        public void ApplyVersion(SemVersion version)
        {
            var yamlStream = new YamlStream();
            var input = new StringReader(File.ReadAllText(FilePath.FullName));
            yamlStream.Load(input);

            var document = yamlStream.Documents.First();
            var documentRootNode = document.RootNode as YamlMappingNode;

            if (documentRootNode is null)
            {
                Log.Warning($"Unable to apply version to: {FilePath.FullName} (no root node)");
                return;
            }

            if (!documentRootNode.Children.ContainsKey("version"))
            {
                documentRootNode.Children.Add("version", version.ToString());
            }
            else
            {
                documentRootNode.Children["version"] = version.ToString();
            }

            File.Copy(FilePath.FullName, $"{FilePath.FullName}.bak", true);
            File.WriteAllText(FilePath.FullName, new Serializer().Serialize(document.RootNode));
        }
    }
}