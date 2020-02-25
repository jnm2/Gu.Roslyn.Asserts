namespace Gu.Roslyn.Asserts.SyntaxAssertions
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;

    internal readonly struct DiagnosticAssertion
    {
        internal DiagnosticAssertion(string diagnosticId, string message, DiagnosticSeverity severity, ImmutableArray<TextSpan> locations)
        {
            this.DiagnosticId = diagnosticId;
            this.Message = message;
            this.Severity = severity;
            this.Locations = locations;
        }

        internal string DiagnosticId { get; }

        internal string Message { get; }

        internal DiagnosticSeverity Severity { get; }

        internal ImmutableArray<TextSpan> Locations { get; }
    }
}
