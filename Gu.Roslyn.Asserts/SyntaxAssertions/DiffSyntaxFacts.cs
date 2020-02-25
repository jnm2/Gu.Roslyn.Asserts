namespace Gu.Roslyn.Asserts.SyntaxAssertions
{
    using System;
    using System.IO;
    using System.Linq;
    using Gu.Roslyn.Asserts.Internals;
    using Microsoft.CodeAnalysis.Text;

    /// <summary>
    /// Source of truth for parsing and formatting diffs that represent code fix results.
    /// </summary>
    internal static class DiffSyntaxFacts
    {
        /// <summary>
        /// Computes the code fix result indicated by the given diff.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when the original text and diff are incompatible or the diff is underspecified.
        /// </exception>
        internal static SourceText ApplyDiff(SourceText originalText, string diff)
        {
            var diffSourceText = SourceText.From(diff).WithoutInitialBlankLine().WithoutFinalBlankLine();

            if (diffSourceText.Lines.Count == 2)
            {
                var firstLine = diffSourceText.Lines[0].ToString();
                var secondLine = diffSourceText.Lines[1].ToString();

                if (firstLine.StartsWith("-", StringComparison.Ordinal) && secondLine.StartsWith("+", StringComparison.Ordinal))
                {
                    var findLineText = GetLineWithoutDiffIndicator(firstLine);
                    var replaceLineText = GetLineWithoutDiffIndicator(secondLine);

                    var foundLines = originalText.Lines.Where(line => line.ToString() == findLineText).ToList();

                    if (foundLines.Count == 0)
                    {
                        throw new ArgumentException("The diff line being removed must be present in the original text:" + Environment.NewLine + findLineText);
                    }

                    if (foundLines.Count > 1)
                    {
                        throw new ArgumentException("The diff line being removed occurs more than once in the original text:" + Environment.NewLine + findLineText);
                    }

                    return originalText.WithChanges(new TextChange(foundLines.Single().Span, replaceLineText));
                }
            }

            throw new NotImplementedException("TODO: full diff capabilities");
        }

        /// <summary>
        /// Formats the assertion message when the actual result does not match the expected result based on a diff from
        /// the original source.
        /// </summary>
        internal static string FormatActualAndExpectedMessage(SourceText originalSource, SourceText actualFixResult, SourceText expectedFixResult)
        {
            // TODO: "Actual fix result diff:" and computed diff from original to actual,
            // suitable for pasting into the test to update it.
            using var message = new StringWriter();

            message.WriteLine("Actual fix result:");
            WriteSkippingFirstLineIfEmptyAndEnsuringEmptyLastLine(actualFixResult, message);
            message.WriteLine();
            message.WriteLine("Expected fix result:");
            WriteSkippingFirstLineIfEmptyAndEnsuringEmptyLastLine(expectedFixResult, message);

            return message.ToString();
        }

        private static string GetLineWithoutDiffIndicator(string diffLine)
        {
            return (diffLine.Length >= 2 && diffLine[1] == ' ' ? " " : null) + diffLine.Substring(1);
        }

        private static void WriteSkippingFirstLineIfEmptyAndEnsuringEmptyLastLine(SourceText sourceText, TextWriter textWriter)
        {
            sourceText.WithoutInitialBlankLine().Write(textWriter);

            if (sourceText.Lines.LastOrDefault() is { Span: { IsEmpty: false } })
            {
                textWriter.WriteLine();
            }
        }
    }
}
