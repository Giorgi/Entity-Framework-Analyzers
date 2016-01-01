using System;
using System.Composition;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace EntityFrameworkAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UsePaginationWithLambdaOverloadAnalyzerCodeFixProvider)), Shared]
    public class UsePaginationWithLambdaOverloadAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(UsePaginationWithLambdaOverloadAnalyzer.DiagnosticId); }
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

            var declaration = invocations.First(syntax =>
            {
                var name = semanticModelAsync.GetSymbolInfo(syntax).Symbol?.Name;
                return name == "Take" || name == "Skip";
            });

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(CodeAction.Create(Resources.ChangeWithLambda, c => LiteralToLambdaAsync(context.Document, declaration, c), "EF1002CodeFixProvider"), diagnostic);
        }

        private async Task<Document> LiteralToLambdaAsync(Document document, InvocationExpressionSyntax invocationExpr, CancellationToken cancellationToken)
        {
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken);

            var literalExpression = invocationExpr.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;

            var variableName = await document.FindAvailabeVariableName(invocationExpr, cancellationToken);

            var lambda = SyntaxFactory.ParseStatement($"Expression<Func<int>> {variableName} = () => {literalExpression.Token.ValueText};")
                                      .WithAdditionalAnnotations(Formatter.Annotation).WithTrailingTrivia(SyntaxFactory.EndOfLine("\r"));
            
            var node = invocationExpr.Parent;

            while (!node.Parent.IsKind(SyntaxKind.Block))
            {
                node = node.Parent;
            }

            documentEditor.InsertBefore(node, lambda);
            documentEditor.ReplaceNode(literalExpression, SyntaxFactory.ParseExpression(variableName));

            var newRoot = documentEditor.GetChangedRoot() as CompilationUnitSyntax;

            newRoot = newRoot.AddUsings("System.Data.Entity", "System.Linq.Expressions");

            return document.WithSyntaxRoot(newRoot);
        }
    }
}