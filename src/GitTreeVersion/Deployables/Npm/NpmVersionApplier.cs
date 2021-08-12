using System.IO;
using System.Text;
using System.Text.Json;
using GitTreeVersion.Paths;
using Semver;

namespace GitTreeVersion.Deployables.Npm
{
    public class NpmVersionApplier : IVersionApplier
    {
        public void ApplyVersion(AbsoluteFilePath filePath, SemVersion version)
        {
            var json = File.ReadAllText(filePath.FullName);
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
            File.WriteAllText(filePath.FullName, resultJson);
        }
    }
}