namespace Gu.Roslyn.Asserts
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using Gu.Roslyn.Asserts.SyntaxAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;

    /// <summary>
    /// Provides assertions against the specified diagnostic analyzer and code fix provider types. Use <see
    /// cref="Create{TDiagnosticAnalyzer, TCodeFixProvider}"/> to obtain an instance.
    /// </summary>
    public sealed partial class DiagnosticFixAssert
    {
        private readonly Func<DiagnosticAnalyzer> getDiagnosticAnalyzer;
        private readonly Func<CodeFixProvider> getCodeFixProvider;

        private DiagnosticFixAssert(Func<DiagnosticAnalyzer> getDiagnosticAnalyzer, Func<CodeFixProvider> getCodeFixProvider)
        {
            this.getDiagnosticAnalyzer = getDiagnosticAnalyzer;
            this.getCodeFixProvider = getCodeFixProvider;
        }

        /// <summary>
        /// Obtains an instance which provides assertions against the specified diagnostic analyzer and code fix
        /// provider types.
        /// </summary>
        /// <typeparam name="TDiagnosticAnalyzer">The diagnostic analyzer to test.</typeparam>
        /// <typeparam name="TCodeFixProvider">The code fix provider to test.</typeparam>
        public static DiagnosticFixAssert Create<TDiagnosticAnalyzer, TCodeFixProvider>()
            where TDiagnosticAnalyzer : DiagnosticAnalyzer, new()
            where TCodeFixProvider : CodeFixProvider, new()
        {
            return new DiagnosticFixAssert(
                () => new TDiagnosticAnalyzer(),
                () => new TCodeFixProvider());
        }

        /// <summary>
        /// Asserts that the analyzer produces no diagnostics for the specified syntax.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when the assertion cannot complete to an invalid argument.
        /// </exception>
        /// <exception cref="AssertException">
        /// Thrown when the assertion completes with a failure result.
        /// </exception>
        public void NoDiagnostics(string syntax)
        {
            var parsed = AssertionSyntaxFacts.ParseSyntaxAssertions(syntax);

            if (parsed.DiagnosticAssertions.Any())
            {
                throw new ArgumentException("No diagnostic assertion comments should be present.", nameof(syntax));
            }

            using var workspace = new AdhocWorkspace();
            var document = CreateProjectAndDocument(workspace, parsed.SourceText);
            var diagnostics = this.GetAllAnalyzerDiagnostics(document);

            if (!diagnostics.IsEmpty)
            {
                throw new AssertException("TODO: serialize diagnostics as comments.");
            }
        }

        /// <summary>
        /// Asserts that the entire set of diagnostics produced by the analyzer matches the diagnostics specified as
        /// comment assertions and that the code provider has no fix for any of them.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when the assertion cannot complete to an invalid argument.
        /// </exception>
        /// <exception cref="AssertException">
        /// Thrown when the assertion completes with a failure result.
        /// </exception>
        public void DiagnosticsWithNoFix(string syntaxWithDiagnosticsAsComments)
        {
            var parsed = AssertionSyntaxFacts.ParseSyntaxAssertions(syntaxWithDiagnosticsAsComments);

            using var workspace = new AdhocWorkspace();
            var document = CreateProjectAndDocument(workspace, parsed.SourceText);
            var diagnostics = this.GetAllAnalyzerDiagnostics(document);

            var assertedValues = GetAssertedValues(parsed.SourceText, parsed.DiagnosticAssertions).ToImmutableHashSet();
            if (assertedValues.SetEquals(diagnostics.Select(GetComparedValues)))
            {
                throw new AssertException("TODO: serialize diagnostics as comments.");
            }

            var codeFixProvider = this.getCodeFixProvider.Invoke();

            var verifier = new NoFixVerifier();

            foreach (var diagnostic in diagnostics)
            {
                codeFixProvider.RegisterCodeFixesAsync(new CodeFixContext(
                    document,
                    diagnostic,
                    verifier.RegisterCodeFix,
                    CancellationToken.None)).GetAwaiter().GetResult();
            }

            if (verifier.ThrownAssertion is { })
            {
                // This was thrown during the code fix provider's call to RegisterCodeFix. The fact that the call to
                // RegisterCodeFixesAsync returned means that the code fix provider swallowed the exception.
                ExceptionDispatchInfo.Capture(verifier.ThrownAssertion).Throw();
            }
        }

        /// <summary>
        /// Asserts that the single diagnostic location selected by a comment assertion is produced by the analyzer and
        /// the code fix provider has exactly one fix for it, having the given title and diff.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when the assertion cannot complete to an invalid argument.
        /// </exception>
        /// <exception cref="AssertException">
        /// Thrown when the assertion completes with a failure result.
        /// </exception>
        public void SingleFix(string syntaxWithFixDiagnosticAsComment, string title, string expectedDiff)
        {
            var parsed = AssertionSyntaxFacts.ParseSyntaxAssertions(syntaxWithFixDiagnosticAsComment);

            if (parsed.DiagnosticAssertions.Sum(d => d.Locations.Length) != 1)
            {
                throw new ArgumentException("The single diagnostic assertion comment with a single location to use for the fix must be in the test syntax.", nameof(syntaxWithFixDiagnosticAsComment));
            }

            var expectedFixResult = DiffSyntaxFacts.ApplyDiff(parsed.SourceText, expectedDiff);

            using var workspace = new AdhocWorkspace();
            var document = CreateProjectAndDocument(workspace, parsed.SourceText);
            var diagnostics = this.GetAllAnalyzerDiagnostics(document);

            var assertedValues = GetAssertedValues(parsed.SourceText, parsed.DiagnosticAssertions).Single();

            var matchingDiagnostics = (
                from d in diagnostics
                where GetComparedValues(d) == assertedValues
                select d).ToList();

            if (matchingDiagnostics.Count != 1)
            {
                throw new AssertException("TODO: serialize diagnostics as comments.");
            }

            var codeFixProvider = this.getCodeFixProvider.Invoke();

            var verifier = new SingleFixVerifier(document.Id, parsed.SourceText, title, matchingDiagnostics.Single(), expectedFixResult);

            codeFixProvider.RegisterCodeFixesAsync(new CodeFixContext(
                document,
                diagnostics.Single(),
                verifier.RegisterCodeFix,
                CancellationToken.None)).GetAwaiter().GetResult();

            if (verifier.ThrownAssertion is { })
            {
                // This was thrown during the code fix provider's call to RegisterCodeFix. The fact that the call to
                // RegisterCodeFixesAsync returned means that the code fix provider swallowed the exception.
                ExceptionDispatchInfo.Capture(verifier.ThrownAssertion).Throw();
            }

            if (!verifier.ActionWasRegistered)
            {
                throw new AssertException($"No code fix was registered with the title '{title}'.");
            }
        }

        // TODO: Replace with Gu.Roslyn.Assert equivalents?
        private ImmutableArray<Diagnostic> GetAllAnalyzerDiagnostics(Document document)
        {
            var compilation = document.Project.GetCompilationAsync().GetAwaiter().GetResult();

            return compilation
                .WithAnalyzers(ImmutableArray.Create(this.getDiagnosticAnalyzer.Invoke()))
                .GetAnalyzerDiagnosticsAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        // TODO: Replace with Gu.Roslyn.Assert equivalents?
        private static Document CreateProjectAndDocument(AdhocWorkspace workspace, SourceText sourceText)
        {
            return workspace
                .AddProject("Project", LanguageNames.CSharp)
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddDocument("Document.cs", sourceText);
        }

        private static (string DiagnosticId, LinePositionSpan Span, string Message, DiagnosticSeverity Severity) GetComparedValues(Diagnostic diagnostic)
        {
            return (diagnostic.Id, diagnostic.Location.GetLineSpan().Span, diagnostic.GetMessage(), diagnostic.Severity);
        }

        private static IEnumerable<(string DiagnosticId, LinePositionSpan Span, string Message, DiagnosticSeverity Severity)> GetAssertedValues(SourceText sourceText, ImmutableArray<DiagnosticAssertion> diagnosticsAsserted)
        {
            return
                from d in diagnosticsAsserted
                from span in d.Locations
                select (d.DiagnosticId, sourceText.Lines.GetLinePositionSpan(span), d.Message, d.Severity);
        }
    }
}
