using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GitTreeVersion.Formatting;

internal abstract class GridResult
{
    protected abstract IEnumerable<string> GetColumnNames();

    protected abstract IEnumerable<IEnumerable<object>> GetRows();

    public string RenderAs(OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Json => RenderAsJson(),
            OutputFormat.Text => RenderAsText(),
            _ => throw new NotImplementedException()
        };
    }

    private string RenderAsJson()
    {
        IEnumerable<JsonObject> ConvertRowsToJsonObjects()
        {
            var columnNames = GetColumnNames().ToArray();

            foreach (var row in GetRows())
            {
                var columns = row.ToArray();
                if (columnNames.Length != columns.Length)
                {
                    throw new InvalidOperationException("Column count mismatch");
                }

                var jsonProperties = columnNames.Zip(columns)
                    .ToDictionary(x => x.First, x => JsonValue.Create(x.Second.ToString()) as JsonNode);

                yield return new JsonObject(jsonProperties);
            }
        }

        var jsonObjects = ConvertRowsToJsonObjects().ToArray<JsonNode?>();
        var result = new JsonArray(jsonObjects);

        return result.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private string RenderAsText()
    {
        IEnumerable<string> JoinColumns()
        {
            var columnNames = GetColumnNames().ToArray();

            foreach (var row in GetRows())
            {
                var columns = row.ToArray();
                if (columnNames.Length != columns.Length)
                {
                    throw new InvalidOperationException("Column count mismatch");
                }

                yield return string.Join(';', columns);
            }
        }

        return string.Join(Environment.NewLine, JoinColumns());
    }
}

