using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EntityFrameworkAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UsePaginationWithLambdaOverloadAnalyzer : DiagnosticAnalyzer
    {
        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.UsePaginationLambdaOverloadTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.UsePaginationLambdaOverloadMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.UsePaginationLambdaOverloadDescription), Resources.ResourceManager, typeof(Resources));
        internal const string Category = "Usage";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Diagnostics.UsePaginationWithLambdaOverloadAnalyzerDiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = context.Node as InvocationExpressionSyntax;

            if (invocationExpr == null)
            {
                return;
            }

            var memberSymbol = context.SemanticModel.GetSymbolInfo(invocationExpr).Symbol;

            if (memberSymbol?.Name != "Take" && memberSymbol?.Name != "Skip")
            {
                return;
            }

            var classSymbol = memberSymbol.ContainingSymbol as INamedTypeSymbol;
            if (classSymbol != null && !classSymbol.ToDisplayString().StartsWith("System.Linq.Queryable"))
            {
                return;
            }

            var argumentList = invocationExpr.ArgumentList;

            if (argumentList == null || argumentList.Arguments.Count == 0)
            {
                return;
            }

            var location = invocationExpr.GetInvocationLocationWithArguments();

            var diagnostic = Diagnostic.Create(Rule, location, memberSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}