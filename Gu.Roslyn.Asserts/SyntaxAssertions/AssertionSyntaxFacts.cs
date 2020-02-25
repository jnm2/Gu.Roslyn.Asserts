namespace Gu.Roslyn.Asserts.SyntaxAssertions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Gu.Roslyn.Asserts.Internals;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Text;

    /// <summary>
    /// Source of truth for parsing and formatting C# syntax that contains assertions as comments.
    /// </summary>
    internal static class AssertionSyntaxFacts
    {
        /// <summary>
        /// Separates comment assertions from the syntax into a parsed model.
        /// </summary>
        internal static SourceTextWithAssertions ParseSyntaxAssertions(string syntaxWithAssertionsAsComments)
        {
            var sourceText = SourceText.From(syntaxWithAssertionsAsComments);
            var syntaxRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();

            var diagnosticAssertions = ImmutableArray.CreateBuilder<DiagnosticAssertion>();

            var commentRemovals = new List<TextChange>();
            var currentEditOffset = 0;

            foreach (var commentTrivia in syntaxRoot.DescendantTrivia())
            {
                if (GetMarkSpansOnLastLine(commentTrivia) is var (markSpans, markSpanLineNumber)
                    && markSpanLineNumber < sourceText.Lines.Count - 1)
                {
                    var triviaList = GetParentTriviaList(commentTrivia);

                    // TODO: Support single-line
                    // TODO: Permit colon
                    // TODO: Permit severity or message to be missing
                    if (!(GetPreviousCommentLineText(sourceText, markSpanLineNumber, triviaList) is var (previousCommentLineText, startingCommentLine)))
                    {
                        continue;
                    }

                    if (!TryParsePreviousCommentLineText(previousCommentLineText, out var diagnosticId, out var message, out var severity))
                    {
                        continue;
                    }

                    var spanToRemove = TextSpan.FromBounds(
                        sourceText.Lines[startingCommentLine].Start,
                        sourceText.Lines[markSpanLineNumber].EndIncludingLineBreak);
                    commentRemovals.Add(new TextChange(spanToRemove, newText: string.Empty));
                    currentEditOffset -= spanToRemove.Length;

                    var diagnosticSpans = ImmutableArray.CreateBuilder<TextSpan>(markSpans.Length);
                    var nextLineEndPosition = sourceText.Lines[markSpanLineNumber + 1].End;

                    foreach (var markSpan in markSpans)
                    {
                        var lineSpan = sourceText.Lines.GetLinePositionSpan(markSpan.Span);
                        Trace.Assert(lineSpan.Start.Line == markSpanLineNumber && lineSpan.End.Line == markSpanLineNumber);

                        var nextLineTextSpan = sourceText.Lines.GetTextSpan(lineSpan.AddLineDelta(1));
                        if (nextLineTextSpan.End > nextLineEndPosition)
                        {
                            throw new ArgumentException($"Marked ‘{markSpan.Mark}’ span on line {lineSpan.Start.Line} points past the end of the next line.", nameof(syntaxWithAssertionsAsComments));
                        }

                        diagnosticSpans.Add(new TextSpan(nextLineTextSpan.Start + currentEditOffset, nextLineTextSpan.Length));
                    }

                    diagnosticAssertions.Add(new DiagnosticAssertion(
                        diagnosticId,
                        message,
                        severity,
                        diagnosticSpans.MoveToImmutable()));
                }
            }

            var newText = sourceText.WithChanges(commentRemovals);

            return new SourceTextWithAssertions(newText, diagnosticAssertions.ToImmutable());
        }

        private static bool TryParsePreviousCommentLineText(
            string? text,
            [NotNullWhen(true)] out string? diagnosticId,
            [NotNullWhen(true)] out string? message,
            out DiagnosticSeverity severity)
        {
            if (!string.IsNullOrEmpty(text))
            {
                // Maybe if hidden comes up, could surround with parens?
                if (TryParseSeverityEmoji(text[0], out severity))
                {
                    var remainingText = text.AsSpan(1).TrimStart();
                    var endOfIdIndex = remainingText.IndexOf(' ');
                    if (endOfIdIndex != -1)
                    {
                        diagnosticId = remainingText.Slice(0, endOfIdIndex).ToString();
                        message = remainingText.Slice(endOfIdIndex + 1).Trim().ToString();
                        return true;
                    }
                }
            }

            diagnosticId = null;
            message = null;
            severity = default;
            return false;
        }

        private static bool TryParseSeverityEmoji(char firstCharacter, out DiagnosticSeverity severity)
        {
            switch (firstCharacter)
            {
                case '❌':
                    severity = DiagnosticSeverity.Error;
                    return true;
                case '⚠':
                    severity = DiagnosticSeverity.Warning;
                    return true;
                case 'ℹ':
                    severity = DiagnosticSeverity.Info;
                    return true;
                default:
                    severity = default;
                    return false;
            }
        }

        private static (string Text, int StartingLine)? GetPreviousCommentLineText(SourceText sourceText, int markSpanLine, SyntaxTriviaList triviaList)
        {
            var previousCommentTextLines = new List<string>();
            var startingLine = markSpanLine - 1;

            for (var previousLine = markSpanLine - 1; previousLine >= 0; previousLine--)
            {
                var lineSpan = sourceText.Lines[previousLine].Span;

                if (GetSingleCommentContentSpan(triviaList, lineSpan) is { } wholeLineCommentContentSpan)
                {
                    var wholeLineContent = sourceText.ToString(wholeLineCommentContentSpan).Trim();
                    if (wholeLineContent.Length > 0)
                    {
                        previousCommentTextLines.Add(wholeLineContent);
                    }

                    startingLine = previousLine;
                }
                else
                {
                    break;
                }
            }

            if (previousCommentTextLines.Any())
            {
                return (string.Join(" ", Enumerable.Reverse(previousCommentTextLines)), startingLine);
            }

            return null;
        }

        private static TextSpan? GetSingleCommentContentSpan(SyntaxTriviaList triviaList, TextSpan span)
        {
            return triviaList.TrySingle(t => t.Span.OverlapsWith(span), out var singleTriviaOnLine)
                ? GetCommentContentSpan(singleTriviaOnLine)?.Overlap(span)
                : null;
        }

        private static SyntaxTriviaList GetParentTriviaList(SyntaxTrivia trivia)
        {
            var token = trivia.Token;

            return token.LeadingTrivia.Span.Contains(trivia.SpanStart)
                ? token.LeadingTrivia
                : token.TrailingTrivia;
        }

        private static (ImmutableArray<(char Mark, TextSpan Span)>, int LineNumber)? GetMarkSpansOnLastLine(SyntaxTrivia commentTrivia)
        {
            if (GetCommentContentSpan(commentTrivia) is { IsEmpty: false } contentSpan)
            {
                var builder = ImmutableArray.CreateBuilder<(char Mark, TextSpan Span)>();

                var sourceText = commentTrivia.SyntaxTree.GetText();
                var lastLineOfCommentContent = GetLastLineOfSpan(contentSpan, sourceText);

                for (var index = lastLineOfCommentContent.Span.Start; index < lastLineOfCommentContent.Span.End;)
                {
                    var character = sourceText[index];
                    if (character == '↓')
                    {
                        var markStartIndex = index;

                        do
                        {
                            index++;
                        }
                        while (index < lastLineOfCommentContent.Span.End && sourceText[index] == character);

                        builder.Add((character, TextSpan.FromBounds(markStartIndex, index)));
                    }
                    else
                    {
                        index++;
                    }
                }

                if (builder.Any())
                {
                    return (builder.ToImmutable(), lastLineOfCommentContent.LineNumber);
                }
            }

            return null;
        }

        private static (TextSpan Span, int LineNumber) GetLastLineOfSpan(TextSpan contentSpan, SourceText sourceText)
        {
            var lastLine = sourceText.Lines.GetLineFromPosition(contentSpan.End);

            return (
                TextSpan.FromBounds(Math.Max(lastLine.Start, contentSpan.Start), contentSpan.End),
                lastLine.LineNumber);
        }

        private static TextSpan? GetCommentContentSpan(SyntaxTrivia commentTrivia)
        {
            return commentTrivia.Kind() switch
            {
                SyntaxKind.MultiLineCommentTrivia => TextSpan.FromBounds(commentTrivia.Span.Start + 2, commentTrivia.Span.End - 2),
                SyntaxKind.SingleLineCommentTrivia => TextSpan.FromBounds(commentTrivia.Span.Start + 2, commentTrivia.Span.End),
                _ => null,
            };
        }
    }
}
