using System;
using System.Collections;
using System.Collections.Generic;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseIncludeWithLambdaAnalyzerCodeFixProvider)), Shared]
    public class UseIncludeWithLambdaAnalyzerCodeFixProvider : CodeFixProvider
    {
        internal static string title = (new LocalizableResourceString(nameof(Resources.ChangeWithLambda), Resources.ResourceManager, typeof(Resources))).ToString();
        private const string SystemDataEntityNamespace = "System.Data.Entity";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(UseIncludeWithLambdaAnalyzer.DiagnosticId); }
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

            var declaration = invocations.First(syntax => (semanticModelAsync.GetSymbolInfo(syntax).Symbol)?.Name == "Include");

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(CodeAction.Create(title, c => LiteralToLambdaAsync(context.Document, declaration, c), "EF1000CodeFixProvider"), diagnostic);
        }

        private async Task<Document> LiteralToLambdaAsync(Document document, InvocationExpressionSyntax invocationExpr, CancellationToken cancellationToken)
        {
            var generatedVariables = new List<string>();

            var lambdaVariableName = await document.FindAvailabeVariableName(invocationExpr, cancellationToken, generatedVariables);

            generatedVariables.Add(lambdaVariableName);

            var argumentList = invocationExpr.ArgumentList;
            var incudePath = argumentList.Arguments[0].Expression;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var method = semanticModel.GetSymbolInfo(invocationExpr).Symbol as IMethodSymbol;
            var underlyingType = (method?.ReceiverType as INamedTypeSymbol)?.TypeArguments[0];

            var paths = incudePath.ToFullString().Trim('"').Split('.');

            var lambdaPath = $"{lambdaVariableName} => {lambdaVariableName}";

            var nestedLevels = 0;
            var previousPropertyIsCollection = false;

            foreach (var path in paths)
            {
                var property = underlyingType?.GetMembers(path).SingleOrDefault(symbol => symbol.Kind == SymbolKind.Property) as IPropertySymbol;

                if (property == null)
                {
                    return document;
                }

                lambdaPath += ".";

                if (previousPropertyIsCollection)
                {
                    var innerLambdaVariableName = await document.FindAvailabeVariableName(invocationExpr, cancellationToken, generatedVariables);
                    generatedVariables.Add(innerLambdaVariableName);

                    lambdaPath += $"Select({innerLambdaVariableName}=>{innerLambdaVariableName}.{path}";
                    nestedLevels++;
                }
                else
                {
                    lambdaPath += path;
                }

                previousPropertyIsCollection = property.Type.AllInterfaces.Any(x => x.MetadataName == typeof(IEnumerable<>).Name);

                // If the property is List<T> or ICollection<T> get the underlying type for next iteration.
                if (previousPropertyIsCollection)
                {
                    underlyingType = (property.Type as INamedTypeSymbol)?.TypeArguments[0];
                }
            }

            lambdaPath += new string(')', nestedLevels);

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
    }
}