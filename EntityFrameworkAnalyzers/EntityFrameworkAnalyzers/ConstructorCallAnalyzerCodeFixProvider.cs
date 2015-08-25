using System.Composition;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityFrameworkAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstructorCallAnalyzerCodeFixProvider)), Shared]
    public class ConstructorCallAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ConstructorCallAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;


            var semanticModelAsync = await context.Document.GetSemanticModelAsync(context.CancellationToken);

            // Find the type declaration identified by the diagnostic.
            var invocations = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>();

            var declaration = invocations.First(syntax => (semanticModelAsync.GetSymbolInfo(syntax).Symbol)?.Name == "Select");

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(CodeAction.Create(Resources.ConvertToEnumerable, c => AddAsEnumerableAsync(context.Document, declaration, c), "EF1001CodeFixProvider"), diagnostic);
        }

        private async Task<Document> AddAsEnumerableAsync(Document document, InvocationExpressionSyntax invocationExpr, CancellationToken cancellationToken)
        {
            var memberAccessExpressionSyntax = invocationExpr.Expression as MemberAccessExpressionSyntax;

            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var accessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, memberAccessExpressionSyntax.Expression, SyntaxFactory.IdentifierName("AsEnumerable"));

            var invocationExpression = SyntaxFactory.InvocationExpression(accessExpression);
            var enumerableMemberAccessExpression = memberAccessExpressionSyntax.WithExpression(invocationExpression);

            root = root.ReplaceNode(invocationExpr, invocationExpr.WithExpression(enumerableMemberAccessExpression));

            return document.WithSyntaxRoot(root);
        }
    }
}