using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using GitTreeVersion.Context;
using GitTreeVersion.Deployables;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;

namespace GitTreeVersion.Commands.Deployable;

public class ListDeployablesCommand : Command
{
    public ListDeployablesCommand() : base("ls", "List deployables")
    {
        Handler = CommandHandler.Create<bool, string?>(Execute);

        AddOption(new Option<bool>("--only-impacted", "Only list deployables that were impacted"));
        AddArgument(new Argument<string?>("path", () => "."));
    }

    private void Execute(bool onlyImpacted, string? path)
    {
        if (path is not null)
        {
            path = Path.GetFullPath(path);
        }

        path ??= Environment.CurrentDirectory;

        var versionGraph = ContextResolver.GetVersionGraph(new AbsoluteDirectoryPath(path));
        var deployables = versionGraph.Deployables.Values.AsEnumerable();

        if (onlyImpacted)
        {
            deployables = FilterImpactedDeployables(deployables, versionGraph, path);
        }

        foreach (var deployable in deployables)
        {
            Console.WriteLine(deployable.FilePath.FullName);
        }
    }

    private static IEnumerable<IDeployable> FilterImpactedDeployables(IEnumerable<IDeployable> deployables, VersionGraph versionGraph, string path)
    {
        var gitDirectory = new GitDirectory(new AbsoluteDirectoryPath(path));
        var(parent1, parent2) = gitDirectory.GetMergeParentCommitHashes();
        var changedFiles = gitDirectory.GitDiffFileNames(parent1, parent2, null)
            .Select(f => Path.Combine(path, f))
            .Select(f => new AbsoluteFilePath(f))
            .ToArray();

        foreach (var deployable in deployables)
        {
            var relatedPaths = GetRelatedPaths(deployable, versionGraph);

            var isImpacted = changedFiles.Any(changedFile => relatedPaths.Any(changedFile.IsInSubPathOf));

            if (!isImpacted)
            {
                continue;
            }

            yield return deployable;
        }
    }

    private static IEnumerable<AbsoluteDirectoryPath> GetRelatedPaths(IDeployable deployable, VersionGraph versionGraph)
    {
        var deployableQueue = new Queue<IDeployable>(new[] { deployable });
        var relatedDeployables = new HashSet<AbsoluteFilePath>();

        while (deployableQueue.TryDequeue(out var currentDeployable))
        {
            if (!relatedDeployables.Add(currentDeployable.FilePath))
            {
                continue;
            }

            var deployables = deployable.ReferencedDeployablePaths
                .Except(relatedDeployables)
                .Select(fp => versionGraph.Deployables[fp]);

            foreach (var relatedDeployable in deployables)
            {
                deployableQueue.Enqueue(relatedDeployable);
            }
        }

        return relatedDeployables.Select(rd => rd.Parent);
    }
}