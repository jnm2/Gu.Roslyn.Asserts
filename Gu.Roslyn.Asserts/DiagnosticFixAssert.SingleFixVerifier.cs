namespace Gu.Roslyn.Asserts
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Threading;
    using Gu.Roslyn.Asserts.SyntaxAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.Text;

    public sealed partial class DiagnosticFixAssert
    {
        private sealed class SingleFixVerifier
        {
            private readonly DocumentId documentId;
            private readonly SourceText originalSource;
            private readonly string title;
            private readonly Diagnostic expectedDiagnostic;
            private readonly SourceText expectedFixResult;

            internal SingleFixVerifier(DocumentId documentId, SourceText originalSource, string title, Diagnostic expectedDiagnostic, SourceText expectedFixResult)
            {
                this.documentId = documentId;
                this.originalSource = originalSource;
                this.title = title;
                this.expectedDiagnostic = expectedDiagnostic;
                this.expectedFixResult = expectedFixResult;
            }

            internal bool ActionWasRegistered { get; private set; }

            internal Exception? ThrownAssertion { get; private set; }

            [DebuggerNonUserCode]
            internal void RegisterCodeFix(CodeAction codeAction, ImmutableArray<Diagnostic> diagnostics)
            {
                if (diagnostics.Length != 1 && ReferenceEquals(diagnostics[0], this.expectedDiagnostic))
                {
                    throw new ArgumentException("Action was registered for a diagnostic that was not passed.", nameof(diagnostics));
                }

                if (codeAction.Title != this.title)
                {
                    throw new ArgumentException($"The code action title is '{codeAction.Title}', but only an action with the title '{this.title}' should be produced.", nameof(codeAction));
                }

                if (this.ActionWasRegistered)
                {
                    throw new ArgumentException("More than one action was registered with the same title.", nameof(codeAction));
                }

                this.ActionWasRegistered = true;

                var operations = codeAction.GetOperationsAsync(CancellationToken.None).GetAwaiter().GetResult();
                if (operations.Length != 1)
                {
                    throw new NotImplementedException("TODO: number of code action operations other than one");
                }

                var operation = operations[0] as ApplyChangesOperation
                    ?? throw new NotImplementedException("TODO: code action operation types other than ApplyChangesOperation");

                var document = operation.ChangedSolution.GetDocument(this.documentId)
                    ?? throw new ArgumentException("The changed solution no longer has the original document.", nameof(codeAction));

                var actualFixResult = document.GetTextAsync().GetAwaiter().GetResult();

                if (!actualFixResult.ContentEquals(this.expectedFixResult))
                {
                    this.ThrownAssertion = new AssertException(DiffSyntaxFacts.FormatActualAndExpectedMessage(
                        this.originalSource,
                        actualFixResult,
                        this.expectedFixResult));

                    throw this.ThrownAssertion;
                }
            }
        }
    }
}
