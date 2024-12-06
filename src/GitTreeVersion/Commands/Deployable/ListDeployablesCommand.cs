using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using GitTreeVersion.Context;
using GitTreeVersion.Deployables;
using GitTreeVersion.Formatting;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Commands.Deployable;

public class ListDeployablesCommand : Command
{
    public ListDeployablesCommand() : base("ls", "List deployables")
    {
        Handler = CommandHandler.Create<bool, bool, OutputFormat, string>(Execute);

        AddOption(new Option<bool>("--only-impacted", "Only list deployables that were impacted"));
        AddOption(new Option<bool>("--output-versions", "Output versions of deployables"));
        AddOption(new Option<OutputFormat>("--format", () => OutputFormat.Text, "Output format"));
        AddArgument(new Argument<string>("path", () => "."));
    }

    private void Execute(
        bool onlyImpacted,
        bool outputVersions,
        OutputFormat format,
        string path)
    {
        path = Path.GetFullPath(path);

        var versionGraph = ContextResolver.GetVersionGraph(new AbsoluteDirectoryPath(path));
        var deployableRecords = versionGraph.Deployables.Values
            .Select(dp => new DeployableRecord(dp));

        if (onlyImpacted)
        {
            deployableRecords = FilterImpactedDeployables(deployableRecords, versionGraph, path);
        }

        if (outputVersions)
        {
            deployableRecords = SetDeployableVersions(deployableRecords, versionGraph);
        }

        var deployableResult = new DeployableResult(deployableRecords, outputVersions);

        Console.WriteLine(deployableResult.RenderAs(format));
    }

    private static IEnumerable<DeployableRecord> FilterImpactedDeployables(IEnumerable<DeployableRecord> deployableRecords, VersionGraph versionGraph, string path)
    {
        var gitDirectory = new GitDirectory(new AbsoluteDirectoryPath(path));
        var(parent1, parent2) = gitDirectory.GetMergeParentCommitHashes();
        var changedFiles = gitDirectory.GitDiffFileNames(parent1, parent2, null)
            .Select(f => Path.Combine(path, f))
            .Select(f => new AbsoluteFilePath(f))
            .ToArray();

        foreach (var record in deployableRecords)
        {
            var (deployable, _) = record;
            var relatedPaths = GetRelatedPaths(deployable, versionGraph);

            var isImpacted = changedFiles.Any(changedFile => relatedPaths.Any(changedFile.IsInSubPathOf));

            if (!isImpacted)
            {
                continue;
            }

            yield return record;
        }
    }

    private static IEnumerable<AbsoluteDirectoryPath> GetRelatedPaths(IDeployable deployableRecords, VersionGraph versionGraph)
    {
        var deployableQueue = new Queue<IDeployable>([deployableRecords]);
        var relatedDeployables = new HashSet<AbsoluteFilePath>();

        while (deployableQueue.TryDequeue(out var currentDeployable))
        {
            if (!relatedDeployables.Add(currentDeployable.FilePath))
            {
                continue;
            }

            var deployables = deployableRecords.ReferencedDeployablePaths
                .Except(relatedDeployables)
                .Select(fp => versionGraph.Deployables[fp]);

            foreach (var relatedDeployable in deployables)
            {
                deployableQueue.Enqueue(relatedDeployable);
            }
        }

        return relatedDeployables.Select(rd => rd.Parent);
    }

    private static IEnumerable<DeployableRecord> SetDeployableVersions(IEnumerable<DeployableRecord> deployableRecords, VersionGraph versionGraph)
    {
        var versionCalculator = new VersionCalculator();

        foreach (var deployable in deployableRecords)
        {
            var versionRoot = versionGraph.DeployableFileVersionRoots[deployable.Deployable.FilePath];
            var version = versionCalculator.GetVersion(versionGraph, versionRoot);

            yield return deployable with{ Version = version.ToString() };
        }
    }

    private class DeployableResult(IEnumerable<DeployableRecord> deployableRecords, bool outputVersions) : GridResult
    {
        protected override IEnumerable<string> GetColumnNames()
        {
            yield return "deployableFilePath";

            if (outputVersions)
            {
                yield return "version";
            }
        }

        protected override IEnumerable<IEnumerable<object>> GetRows()
        {
            foreach (var (deployable, version) in deployableRecords)
            {
                string[] output = outputVersions
                    ? [deployable.FilePath.FullName, version ?? string.Empty]
                    : [deployable.FilePath.FullName];

                yield return output;
            }
        }
    }

    internal record DeployableRecord(IDeployable Deployable, string? Version = null);
}