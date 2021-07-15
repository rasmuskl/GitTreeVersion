namespace GitTreeVersion
{
    public class RepositoryContext
    {
        public string RepositoryRootPath { get; }

        public string VersionRootPath { get; }

        public VersionConfig VersionConfig { get; }

        public RepositoryContext(string repositoryRootPath, string versionRootPath, VersionConfig versionConfig)
        {
            RepositoryRootPath = repositoryRootPath;
            VersionRootPath = versionRootPath;
            VersionConfig = versionConfig;
        }
    }
}