using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using GitTreeVersion.Paths;
using NUnit.Framework;

namespace GitTreeVersion.Tests;

internal class DeployableScenarios : GitTestBase
{
    private const string Deployable1Path = "Deployable1";
    private static readonly string Deployable1FileName = Path.Combine(Deployable1Path, "Deployable1.csproj");
    private static readonly string Deployable2FileName = Path.Combine("Deployable2", "Deployable2.csproj");
    private const string ReferencedDeployablePath = "GitTreeVersion";
    private static readonly string ReferencedDeployableFileName = Path.Combine(ReferencedDeployablePath, "GitTreeVersion.csproj");

    [Test]
    public async Task ListDeployables_OutputAllDeployablesInRepository()
    {
        var repositoryPath = CreateGitRepositoryWithDeployables();

        var deployables = await ExecuteCommand("deployable", "ls", repositoryPath.ToString());

        deployables.Should().HaveCount(3);
        deployables.Should().ContainSingle(x => x.EndsWith(Deployable1FileName));
        deployables.Should().ContainSingle(x => x.EndsWith(Deployable2FileName));
        deployables.Should().ContainSingle(x => x.EndsWith(ReferencedDeployableFileName));
    }

    [Test]
    public async Task ListImpactedDeployables_WithoutImpactingChanges_DoesNotOutput()
    {
        var repositoryPath = CreateGitRepositoryWithDeployables();

        var branch = CreateBranch(repositoryPath);
        CommitNewFile(repositoryPath);

        MergeBranchToMaster(repositoryPath, branch);

        var deployables = await ExecuteCommand("deployable", "ls", "--only-impacted", repositoryPath.ToString());

        deployables.Should().BeEmpty();
    }

    [Test]
    public async Task ListImpactedDeployables_WithImpactedOnOneDeployable_OutputsImpactedDeployable()
    {
        var repositoryPath = CreateGitRepositoryWithDeployables();

        var branch = CreateBranch(repositoryPath);
        var deployable1File = repositoryPath.CombineToFile(Deployable1Path, "file.cs");
        await File.WriteAllTextAsync(deployable1File.FullName, Guid.NewGuid().ToString());
        CommitFile(repositoryPath, deployable1File);

        MergeBranchToMaster(repositoryPath, branch);

        var deployables = await ExecuteCommand("deployable", "ls", "--only-impacted", repositoryPath.ToString());

        deployables.Should().HaveCount(1);
        deployables.Should().ContainSingle(x => x.EndsWith(Deployable1FileName));
    }

    [Test]
    public async Task ListImpactedDeployables_WithImpactedOnReferencedDeployable_OutputsAllImpactedDeployables()
    {
        var repositoryPath = CreateGitRepositoryWithDeployables();

        var branch = CreateBranch(repositoryPath);
        var referencedDeployableFile = repositoryPath.CombineToFile(ReferencedDeployablePath, "file.cs");
        await File.WriteAllTextAsync(referencedDeployableFile.FullName, Guid.NewGuid().ToString());
        CommitFile(repositoryPath, referencedDeployableFile);

        MergeBranchToMaster(repositoryPath, branch);

        var deployables = await ExecuteCommand("deployable", "ls", "--only-impacted", repositoryPath.ToString());

        deployables.Should().HaveCount(2);
        deployables.Should().ContainSingle(x => x.EndsWith(Deployable2FileName));
        deployables.Should().ContainSingle(x => x.EndsWith(ReferencedDeployableFileName));
    }

    private AbsoluteDirectoryPath CreateGitRepositoryWithDeployables()
    {
        var repositoryPath = CreateGitRepository();
        var deployablePath1 = new AbsoluteFilePath(
            Path.Combine(repositoryPath.ToString(), Deployable1FileName));
        Directory.CreateDirectory(Path.GetDirectoryName(deployablePath1.FullName)!);

        File.WriteAllText(deployablePath1.FullName, ResourceReader.MinimalCsproj);
        CommitFile(repositoryPath, deployablePath1);

        var deployablePath2 = new AbsoluteFilePath(
            Path.Combine(repositoryPath.ToString(), Deployable2FileName));
        Directory.CreateDirectory(Path.GetDirectoryName(deployablePath2.FullName)!);

        File.WriteAllText(deployablePath2.FullName, ResourceReader.GitTreeVersionTestsCsproj);
        CommitFile(repositoryPath, deployablePath2);

        var referencedDeployable = new AbsoluteFilePath(
            Path.Combine(repositoryPath.ToString(), ReferencedDeployableFileName));
        Directory.CreateDirectory(Path.GetDirectoryName(referencedDeployable.FullName)!);

        File.WriteAllText(referencedDeployable.FullName, ResourceReader.MinimalCsproj);
        CommitFile(repositoryPath, referencedDeployable);

        return repositoryPath;
    }

    private static async Task<string[]> ExecuteCommand(params string[] args)
    {
        var originalIsDebug = Log.IsDebug;
        var originalOutWriter = Console.Out;
        var originalErrorWriter = Console.Error;

        try
        {
            Log.IsDebug = false;
            await using var outWriter = new StringWriter();
            await using var errorWriter = new StringWriter();

            Console.SetOut(outWriter);
            Console.SetError(errorWriter);

            await Program.Main(args);

            errorWriter.ToString().Should().BeNullOrEmpty();
            return outWriter.ToString().SplitOutput();
        }
        finally
        {
            Log.IsDebug = originalIsDebug;
            Console.SetOut(originalOutWriter);
            Console.SetError(originalErrorWriter);
        }
    }
}