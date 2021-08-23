using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GitTreeVersion.BuildEnvironments;
using GitTreeVersion.Context;
using GitTreeVersion.Git;
using GitTreeVersion.Paths;
using LibGit2Sharp;
using Semver;

namespace GitTreeVersion.Tests
{
    public class GitTestBase
    {
        public void MoveAndCommitFile(AbsoluteDirectoryPath repositoryPath, AbsoluteFilePath sourceFile, AbsoluteFilePath targetFile)
        {
            var gitDirectory = new GitDirectory(repositoryPath);

            File.Move(sourceFile.ToString(), targetFile.ToString());
            gitDirectory.RunGit("add", "--all");

            var relativeSourcePath = Path.GetRelativePath(repositoryPath.FullName, sourceFile.ToString());
            var relativeTargetPath = Path.GetRelativePath(repositoryPath.FullName, targetFile.ToString());

            gitDirectory.RunGit("commit", "--all", "--message", $"move {relativeSourcePath} to {relativeTargetPath}");
        }

        public IEnvironmentAccessor SimulateAzureDevOpsPullRequest(AbsoluteDirectoryPath repositoryPath, string branchName, int pullRequestId)
        {
            var environmentVariables = new Dictionary<string, string?>();

            var gitDirectory = new GitDirectory(repositoryPath);
            var currentCommit = gitDirectory.RunGit("rev-parse", branchName).Trim();

            var pullRef = $"pull/{pullRequestId}/pull";
            var mergeRef = $"pull/{pullRequestId}/merge";
            environmentVariables.Add("TF_BUILD", "True");
            environmentVariables.Add("SYSTEM_PULLREQUEST_PULLREQUESTID", pullRequestId.ToString());

            // Source: https://stackoverflow.com/a/42634501
            gitDirectory.RunGit("update-ref", pullRef, currentCommit);
            gitDirectory.RunGit("checkout", "master");
            gitDirectory.RunGit("merge", "--no-ff", pullRef);
            gitDirectory.RunGit("update-ref", mergeRef, "HEAD");
            gitDirectory.RunGit("checkout", mergeRef);

            // git status at this point gives:
            //
            // (local)
            // HEAD detached at pull/42/merge
            // nothing to commit, working tree clean
            //
            // (Azure Pipelines)
            // HEAD detached at pull/42/merge
            // nothing to commit, working tree clean

            return new DictionaryEnvironmentAccessor(environmentVariables);
        }

        public void MergeBranchToMaster(AbsoluteDirectoryPath repositoryPath, string branchName, bool fastForward = false)
        {
            var gitDirectory = new GitDirectory(repositoryPath);

            gitDirectory.RunGit("checkout", "master");
            gitDirectory.RunGit("merge", fastForward ? "--ff" : "--no-ff", branchName);
        }

        public string CreateBranch(AbsoluteDirectoryPath repositoryPath, string? branchName = null)
        {
            branchName ??= Guid.NewGuid().ToString();
            var gitDirectory = new GitDirectory(repositoryPath);
            gitDirectory.RunGit("checkout", "-b", branchName);
            return branchName;
        }

        public void CommitNewFile(AbsoluteDirectoryPath repositoryPath, DateTimeOffset? commitTime = null, string? commitMessage = null)
        {
            var fileName = Path.Combine(repositoryPath.ToString(), $"file-{Guid.NewGuid()}");
            File.WriteAllText(fileName, Guid.NewGuid().ToString());

            using var repository = new Repository(repositoryPath.ToString());

            var relativeFilePath = Path.GetRelativePath(repositoryPath.ToString(), fileName);
            repository.Index.Add(relativeFilePath);
            repository.Index.Write();

            var signature = new Signature(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), commitTime.GetValueOrDefault(DateTimeOffset.UtcNow));
            repository.Commit(commitMessage ?? $"add new file {relativeFilePath}", signature, signature);
        }

        public void CommitFile(AbsoluteDirectoryPath repositoryPath, AbsoluteFilePath filePath, DateTimeOffset? commitTime = null, string? commitMessage = null)
        {
            using var repository = new Repository(repositoryPath.ToString());

            repository.Index.Add(Path.GetRelativePath(repositoryPath.ToString(), filePath.ToString()));
            repository.Index.Write();

            var signature = new Signature(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), commitTime.GetValueOrDefault(DateTimeOffset.UtcNow));
            repository.Commit(commitMessage ?? Guid.NewGuid().ToString(), signature, signature);
        }

        public AbsoluteDirectoryPath CreateGitRepository()
        {
            var repositoryPath = CreateEmptyDirectory();
            Repository.Init(repositoryPath.ToString());
            return repositoryPath;
        }

        public AbsoluteDirectoryPath CreateEmptyDirectory()
        {
            var repositoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(repositoryPath);
            return new AbsoluteDirectoryPath(repositoryPath);
        }

        public void CommitVersionConfig(AbsoluteDirectoryPath repositoryPath, VersionConfig versionConfig, string? commitMessage = null)
        {
            WriteVersionConfig(repositoryPath, versionConfig);

            var gitDirectory = new GitDirectory(repositoryPath);
            gitDirectory.RunGit("add", "--all");
            gitDirectory.RunGit("commit", "--all", "--message", commitMessage ?? Guid.NewGuid().ToString());
        }

        public AbsoluteFilePath WriteVersionConfig(AbsoluteDirectoryPath repositoryPath, VersionConfig versionConfig)
        {
            var filePath = Path.Combine(repositoryPath.ToString(), ContextResolver.VersionConfigFileName);
            File.WriteAllText(filePath, JsonSerializer.Serialize(versionConfig, JsonOptions.DefaultOptions));

            return new AbsoluteFilePath(filePath);
        }

        public static SemVersion CalculateVersion(AbsoluteDirectoryPath repositoryPath)
        {
            var versionGraph = ContextResolver.GetVersionGraph(repositoryPath);
            return new VersionCalculator().GetVersion(versionGraph, versionGraph.VersionRootPath);
        }
    }
}