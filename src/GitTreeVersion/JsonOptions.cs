using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitTreeVersion
{
    public static class JsonOptions
    {
        public static readonly JsonSerializerOptions DefaultOptions = new() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter() } };
    }
}