using System;
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
		private const string SystemDataEntityNamespace = "System.Data.Entity";

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
			var lambdaVariableName = await FindAvailabeVariableName(document, invocationExpr, cancellationToken);

			var argumentList = invocationExpr.ArgumentList;
			var incudePath = argumentList.Arguments[0].Expression;

			var lambdaPath = string.Format("{0} => {0}.{1}", lambdaVariableName, incudePath.ToFullString().Trim('"'));

			var lambdaExpression = SyntaxFactory.ParseExpression(lambdaPath)
												.WithAdditionalAnnotations(Formatter.Annotation);

			var stringLiteralExpression = invocationExpr.ArgumentList.Arguments[0].Expression;

			var root = await document.GetSyntaxRootAsync(cancellationToken) as CompilationUnitSyntax;
			var newRoot = root.ReplaceNode(stringLiteralExpression, lambdaExpression);

			var needsUsing = !newRoot.ChildNodes().OfType<UsingDirectiveSyntax>().Any(u => u.Name.ToString().Equals(SystemDataEntityNamespace));

			if (needsUsing)
			{
				var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(SystemDataEntityNamespace));
				newRoot = newRoot.AddUsings(usingDirective).WithAdditionalAnnotations(Formatter.Annotation);
			}

			var newDocument = document.WithSyntaxRoot(newRoot);
			return newDocument;
		}

		private static async Task<string> FindAvailabeVariableName(Document document, SyntaxNode invocationExpr, CancellationToken cancellationToken)
		{
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

			int currentChar = 1;
			do
			{
				var name = ToVariableName(currentChar);
				var immutableArray = semanticModel.LookupSymbols(invocationExpr.SpanStart, name: name);

				if (immutableArray.Length == 0)
				{
					return name;
				}

				if (immutableArray.All(symbol => symbol.Kind != SymbolKind.Local && symbol.Kind != SymbolKind.Parameter))
				{
					return name;
				}

				currentChar++;
			} while (true);
		}

		private static string ToVariableName(int number)
		{
			var dividend = number;
			var result = string.Empty;

			while (dividend > 0)
			{
				var modulo = (dividend - 1) % 26;
				result = Convert.ToChar('a' + modulo) + result;
				dividend = (dividend - modulo) / 26;
			}

			return result;
		}
	}
}