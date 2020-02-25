namespace Gu.Roslyn.Asserts
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;

    public sealed partial class DiagnosticFixAssert
    {
        private sealed class NoFixVerifier
        {
            internal Exception? ThrownAssertion { get; private set; }

            [DebuggerNonUserCode]
            internal void RegisterCodeFix(CodeAction codeAction, ImmutableArray<Diagnostic> diagnostics)
            {
                this.ThrownAssertion = new AssertException("Expected no code fix to be registered.");

                throw this.ThrownAssertion;
            }
        }
    }
}
