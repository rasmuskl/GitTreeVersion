using System;
using System.IO;
using System.Reflection;

namespace GitTreeVersion.Tests
{
    public static class ResourceReader
    {
        public static string CatapultCsproj => ReadAsString("Catapult.csproj.xml");
        public static string GitTreeVersionTestsCsproj => ReadAsString("GitTreeVersion.Tests.csproj.xml");

        public static string ReadAsString(string resourceName)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"GitTreeVersion.Tests.Resources.{resourceName}");

            if (stream is null)
            {
                throw new Exception($"Resource not found: {resourceName}");
            }

            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }
    }
}