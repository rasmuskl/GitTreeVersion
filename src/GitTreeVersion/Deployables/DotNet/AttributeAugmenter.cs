using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GitTreeVersion.Deployables.DotNet
{
    public class AttributeAugmenter
    {
        public StringAttribute[] DetectStringAttributes(string contents, StringAttribute[] targetAttributes)
        {
            var matches = new List<StringAttribute>();

            foreach (var targetAttribute in targetAttributes)
            {
                var regex = GetAttributeRegex(targetAttribute);

                if (regex.IsMatch(contents))
                {
                    matches.Add(targetAttribute);
                }
            }

            return matches.ToArray();
        }

        public string EnsureStringAttributes(string contents, StringAttribute[] targetAttributes)
        {
            foreach (var targetAttribute in targetAttributes)
            {
                var regex = GetAttributeRegex(targetAttribute);
                var targetString = $"[assembly: {targetAttribute.AttributeType.FullName}(\"{targetAttribute.Value}\")]{Environment.NewLine}";

                if (regex.IsMatch(contents))
                {
                    contents = regex.Replace(contents, targetString);
                }
                else
                {
                    contents += targetString;
                }
            }

            return contents;
        }

        private Regex GetAttributeRegex(StringAttribute targetAttribute)
        {
            return new Regex($@"^\s*\[\s*assembly\s*:\s*{AttributeTypeToPattern(targetAttribute.AttributeType.FullName!)}\s*\(\s*""[^""]+""\s*\)\s*\]\s*", RegexOptions.Multiline);
        }

        private string AttributeTypeToPattern(string typeName)
        {
            var fragments = typeName.Split(".");
            var result = new StringBuilder();

            for (var i = 0; i < fragments.Length - 1; i++)
            {
                result.Append($@"(?:{fragments[i]}\s*\.\s*)?");
            }

            var className = fragments[^1];

            if (className.EndsWith("Attribute"))
            {
                result.Append(className[..^"Attribute".Length]);
                result.Append(@"(?:Attribute)?");
            }
            else
            {
                result.Append(className);
            }

            return result.ToString();
        }
    }
}