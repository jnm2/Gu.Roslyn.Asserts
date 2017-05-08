﻿namespace Gu.Roslyn.Asserts
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis.Text;

    public static class CodeReader
    {
        public static string FileName(string code)
        {
            string fileName;
            if (code == string.Empty)
            {
                return $"Test.cs";
            }

            var match = Regex.Match(code, @"(class|struct|enum|interface) (?<name>\w+)(<(?<type>\w+)(, ?(?<type>\w+))*>)?", RegexOptions.ExplicitCapture);
            if (!match.Success)
            {
                return "AssemblyInfo.cs";
            }

            //Assert.LessOrEqual(1, matches.Count, "Use class per file, it catches more bugs");
            fileName = match.Groups["name"].Value;
            if (match.Groups["type"].Success)
            {
                var args = string.Join(",",
                    match.Groups["type"]
                              .Captures.OfType<Capture>()
                              .Select(c => c.Value.Trim()));
                fileName += $"{{{args}}}";
            }

            return $"{fileName}.cs";
        }

        public static string Namespace(string code)
        {
            const string nameSpacePattern = @"(?<name>\w+(\.\w+)*)";
            var match = Regex.Match(code, $"namespace {nameSpacePattern}", RegexOptions.ExplicitCapture);
            if (match.Success)
            {
                return match.Groups["name"].Value;
            }

            match = Regex.Match(code, $@"\[assembly: AssemblyTitle\(""{nameSpacePattern}""\)\]", RegexOptions.ExplicitCapture);
            if (match.Success)
            {
                return match.Groups["name"].Value;
            }

            return "Unknown";
        }

        public static IEnumerable<LinePosition> FindDiagnosticsPositions(string code)
        {
            var line = 1;
            var character = 1;
            foreach (var c in code)
            {
                if (c == '\n')
                {
                    line++;
                    character = 1;
                    continue;
                }

                if (c == '↓')
                {
                    yield return new LinePosition(line, character);
                    continue;
                }

                character++;
            }
        }
    }
}