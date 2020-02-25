namespace Gu.Roslyn.Asserts.SyntaxAssertions
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis.Text;

    internal readonly struct SourceTextWithAssertions
    {
        internal SourceTextWithAssertions(SourceText sourceText, ImmutableArray<DiagnosticAssertion> diagnosticAssertions)
        {
            this.SourceText = sourceText;
            this.DiagnosticAssertions = diagnosticAssertions;
        }

        internal SourceText SourceText { get; }

        internal ImmutableArray<DiagnosticAssertion> DiagnosticAssertions { get; }
    }
}
