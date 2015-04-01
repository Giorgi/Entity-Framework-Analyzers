using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EntityFrameworkAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EntityFrameworkAnalyzersAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "EntityFrameworkAnalyzers";

		// You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
		internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
		internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
		internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
		internal const string Category = "Naming";

		internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			// TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
			context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
		}

		private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
		{
			var invocationExpr = context.Node as InvocationExpressionSyntax;

			if (invocationExpr == null)
			{
				return;
			}

			var memberSymbol = context.SemanticModel.GetSymbolInfo(invocationExpr).Symbol as IMethodSymbol;

			if (memberSymbol == null || memberSymbol.Name != "Include")
			{
				return;
			}

			var classSymbol = memberSymbol.ContainingSymbol as INamedTypeSymbol;
			if (classSymbol != null && !classSymbol.ToDisplayString().StartsWith("System.Data.Entity.Infrastructure.DbQuery"))
			{
				return;
			}

			var parameters = memberSymbol.Parameters;

			if (parameters == null || parameters.Length == 0)
			{
				return;
			}

			var parameterTypeName = parameters[0].Type.ContainingNamespace.Name + "." + parameters[0].Type.Name;

			if (parameterTypeName == typeof(string).FullName)
			{
				var diagnostic = Diagnostic.Create(Rule, invocationExpr.GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}