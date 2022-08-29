using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion.Deployables.DotNet
{
    public class DotNetClassicProject : IDeployable
    {
        private readonly XDocument _xDocument;

        public DotNetClassicProject(AbsoluteFilePath filePath, AbsoluteFilePath[] referencedDeployablePaths, XDocument xDocument)
        {
            _xDocument = xDocument;
            FilePath = filePath;
            ReferencedDeployablePaths = referencedDeployablePaths;
        }

        public AbsoluteFilePath FilePath { get; }
        public AbsoluteFilePath[] ReferencedDeployablePaths { get; }

        public void ApplyVersion(SemVersion version, ApplyOptions applyOptions)
        {
            var assemblyInfoFile = FilePath.Parent.CombineToFile("Properties", "AssemblyInfo.cs");
            var solutionInfoFiles = Array.Empty<AbsoluteFilePath>();

            if (_xDocument.Root is not null && !applyOptions.SkipSolutionInfoFiles)
            {
                var compileElements = _xDocument.Root.XPathSelectElements("//*[local-name() = 'Compile']").ToArray();
                solutionInfoFiles = (from element in compileElements
                    let includeAttribute = element.Attribute("Include")
                    where includeAttribute is not null
                    where includeAttribute.Value.Contains("SolutionInfo.cs")
                    from potentialPath in includeAttribute.Value.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    let trimmedPotentialPath = potentialPath.Trim()
                    where trimmedPotentialPath.EndsWith("SolutionInfo.cs")
                    where !Path.IsPathRooted(trimmedPotentialPath)
                    select FilePath.Parent.CombineToFile(trimmedPotentialPath)).ToArray();
            }

            var attributeAugmenter = new AttributeAugmenter();
            var dotnetCompatibleVersion = version.Change(prerelease: null, build: null);
            var targetAttributes = new[]
            {
                new StringAttribute(typeof(AssemblyInformationalVersionAttribute), version.ToString()),
                new StringAttribute(typeof(AssemblyVersionAttribute), dotnetCompatibleVersion.ToString()),
                new StringAttribute(typeof(AssemblyFileVersionAttribute), dotnetCompatibleVersion.ToString()),
            };

            var targetInfoFiles = solutionInfoFiles
                .Concat(new[] { assemblyInfoFile })
                .Where(x => x.Exists)
                .Select(x =>
                {
                    var contents = File.ReadAllText(x.FullName);
                    return (filePath: x, contents, attributes: attributeAugmenter.DetectStringAttributes(contents, targetAttributes));
                })
                .ToArray();

            if (targetInfoFiles.Length == 0)
            {
                Log.Warning($"Neither Properties/AssemblyInfo.cs or SolutionInfo.cs found for project: {FilePath.FullName}");
                return;
            }

            if (Log.IsDebug)
            {
                foreach (var (filePath, _, _) in targetInfoFiles)
                {
                    Log.Debug($"Candidate info file: {filePath}");
                }
            }

            var attributeFileTargets = new Dictionary<StringAttribute, AbsoluteFilePath>();

            foreach (var targetAttribute in targetAttributes)
            {
                var found = false;

                foreach (var (filePath, _, attributes) in targetInfoFiles)
                {
                    if (!attributes.Contains(targetAttribute))
                    {
                        continue;
                    }

                    if (attributeFileTargets.ContainsKey(targetAttribute))
                    {
                        Log.Warning($"Attribute {targetAttribute.AttributeType.Name} found in multiple files. Applying to: {attributeFileTargets[targetAttribute].FullName}");
                        continue;
                    }

                    attributeFileTargets[targetAttribute] = filePath;
                    found = true;
                }

                if (!found)
                {
                    var targetInfoFile = targetInfoFiles.First();
                    attributeFileTargets[targetAttribute] = targetInfoFile.filePath;
                    Log.Debug($"Attribute {targetAttribute.AttributeType.Name} not found in any info file. Appending to: {targetInfoFile.filePath}");
                }
            }

            foreach (var (filePath, contents, _) in targetInfoFiles)
            {
                var attributes = attributeFileTargets
                    .Where(x => x.Value == filePath)
                    .Select(x => x.Key)
                    .ToArray();

                if (attributes.Length == 0)
                {
                    continue;
                }

                var targetContents = attributeAugmenter.EnsureStringAttributes(contents, attributes);

                if (applyOptions.BackupChangedFiles)
                {
                    File.Copy(filePath.FullName, $"{filePath.FullName}.bak", true);
                }

                File.WriteAllText(filePath.FullName, targetContents);
            }
        }
    }
}
