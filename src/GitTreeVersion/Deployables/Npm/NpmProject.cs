using System;
using System.IO;
using System.Text;
using System.Text.Json;
using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion.Deployables.Npm
{
    public class NpmProject : IDeployable
    {
        public NpmProject(AbsoluteFilePath filePath)
        {
            FilePath = filePath;
            ReferencedDeployablePaths = Array.Empty<AbsoluteFilePath>();
        }

        public AbsoluteFilePath FilePath { get; }
        public AbsoluteFilePath[] ReferencedDeployablePaths { get; }

        public void ApplyVersion(SemVersion version)
        {
            var json = File.ReadAllText(FilePath.FullName);
            var versionWritten = false;

            using var memoryStream = new MemoryStream();
            using var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });
            using JsonDocument jsonDocument = JsonDocument.Parse(json);

            jsonWriter.WriteStartObject();

            foreach (var element in jsonDocument.RootElement.EnumerateObject())
            {
                if (element.Name == "version")
                {
                    jsonWriter.WritePropertyName(element.Name);
                    jsonWriter.WriteStringValue(version.ToString());

                    versionWritten = true;
                }
                else
                {
                    element.WriteTo(jsonWriter);
                }
            }

            if (!versionWritten)
            {
                jsonWriter.WritePropertyName("version");
                jsonWriter.WriteStringValue(version.ToString());
            }

            jsonWriter.WriteEndObject();

            jsonWriter.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);

            var resultJson = Encoding.UTF8.GetString(memoryStream.ToArray());
            File.WriteAllText(FilePath.FullName, resultJson);
        }
    }
}