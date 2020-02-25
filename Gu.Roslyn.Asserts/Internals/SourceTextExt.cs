namespace Gu.Roslyn.Asserts.Internals
{
    using System.Linq;
    using Microsoft.CodeAnalysis.Text;

    /// <summary>
    /// Extension methods for <see cref="SourceText"/>.
    /// </summary>
    internal static class SourceTextExt
    {
        /// <summary>
        /// If the initial line is blank, returns an instance without that line.
        /// </summary>
        internal static SourceText WithoutInitialBlankLine(this SourceText sourceText)
        {
            if (sourceText.Lines.FirstOrDefault() is { Span: { IsEmpty: true } } firstLine)
            {
                return sourceText.GetSubText(firstLine.EndIncludingLineBreak);
            }

            return sourceText;
        }

        /// <summary>
        /// If the final line is blank, returns an instance without that line.
        /// </summary>
        internal static SourceText WithoutFinalBlankLine(this SourceText sourceText)
        {
            if (sourceText.Lines.Count >= 2 && sourceText.Lines.Last().Span.IsEmpty)
            {
                return sourceText.GetSubText(TextSpan.FromBounds(0, sourceText.Lines[sourceText.Lines.Count - 2].End));
            }

            return sourceText;
        }
    }
}
