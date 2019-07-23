namespace Gu.Roslyn.Asserts.Analyzers
{
    using System.Collections.Concurrent;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StandardNamesFix))]
    [Shared]
    public class StandardNamesFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.GURA09UseStandardNames.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out MethodDeclarationSyntax methodDeclaration) &&
                    diagnostic.Properties.TryGetValue("before", out var before) &&
                    diagnostic.Properties.TryGetValue("after", out var after))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            $"Replace {before} with {after}",
                            _ => Task.FromResult(
                                context.Document.WithSyntaxRoot(
                                    syntaxRoot.ReplaceNode(
                                        methodDeclaration,
                                        ReplaceRewriter.Update(methodDeclaration, before, after)))),
                            nameof(StandardNamesFix)),
                        diagnostic);
                }
            }
        }

        private class ReplaceRewriter : CSharpSyntaxRewriter
        {
            private static readonly ConcurrentQueue<ReplaceRewriter> Cache = new ConcurrentQueue<ReplaceRewriter>();

            private string before;
            private string after;

            public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                var pattern = $"[^\\w](?<before>{this.before}[^\\w])";
                if (node.IsKind(SyntaxKind.StringLiteralExpression) &&
                    Regex.IsMatch(node.Token.ValueText, pattern))
                {
                    return node.Update(
                        SyntaxFactory.Literal(
                           Regex.Replace(node.Token.Text, this.before, UpdateMatch),
                           Regex.Replace(node.Token.ValueText, this.before, UpdateMatch)));
                }

                return base.VisitLiteralExpression(node);

                string UpdateMatch(Match match)
                {
                    return match.Value.Replace(this.before, this.after);
                }
            }

            internal static SyntaxNode Update(MethodDeclarationSyntax method, string before, string after)
            {
                if (!Cache.TryDequeue(out var rewriter))
                {
                    rewriter = new ReplaceRewriter();
                }

                rewriter.before = before;
                rewriter.after = after;
                var updated = rewriter.Visit(method);
                Cache.Enqueue(rewriter);
                return updated;
            }
        }
    }
}
