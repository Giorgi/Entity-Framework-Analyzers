using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace EntityFrameworkAnalyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EntityFrameworkAnalyzersCodeFixProvider)), Shared]
	public class EntityFrameworkAnalyzersCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(EntityFrameworkAnalyzersAnalyzer.DiagnosticId); }
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

			// Find the type declaration identified by the diagnostic.
			var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
			
			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(CodeAction.Create("Change with lamda", c => LiteralToLambdaAsync(context.Document, declaration, c)), diagnostic);
		}

		private async Task<Document> LiteralToLambdaAsync(Document document, InvocationExpressionSyntax invocationExpr, CancellationToken cancellationToken)
		{
			var argumentList = invocationExpr.ArgumentList;
			var incudePath = argumentList.Arguments[0].Expression;

			var lambdaPath = string.Format("a => a.{0}", incudePath.ToFullString().Trim('"'));

			var lambdaExpression = SyntaxFactory.ParseExpression(lambdaPath)
									   .WithAdditionalAnnotations(Formatter.Annotation);

			var stringLiteralExpression = invocationExpr.ArgumentList.Arguments[0].Expression;

			var root = await document.GetSyntaxRootAsync(cancellationToken);
			var newRoot = root.ReplaceNode(stringLiteralExpression, lambdaExpression);
			var newDocument = document.WithSyntaxRoot(newRoot);

			return newDocument;
		}
	}
}