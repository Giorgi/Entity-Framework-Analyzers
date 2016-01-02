using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace EntityFrameworkAnalyzers
{
    static class RoslynUtils
    {
        internal static CompilationUnitSyntax AddUsings(this CompilationUnitSyntax compilationUnit, params string[] usings)
        {
            foreach (var @using in usings)
            {
                var needsUsing = !compilationUnit.ChildNodes().OfType<UsingDirectiveSyntax>().Any(u => u.Name.ToString().Equals(@using));

                if (needsUsing)
                {
                    var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(@using));
                    compilationUnit = compilationUnit.AddUsings(usingDirective).WithAdditionalAnnotations(Formatter.Annotation);
                }
            }

            return compilationUnit;
        }

        internal static async Task<string> FindAvailabeVariableName(this Document document, SyntaxNode scope, CancellationToken cancellationToken)
        {
            return await FindAvailabeVariableName(document, scope, cancellationToken, new List<string>());
        }

        internal static async Task<string> FindAvailabeVariableName(this Document document, SyntaxNode scope, CancellationToken cancellationToken, List<string> blackList)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            
            int currentChar = 1;
            do
            {
                var name = ToVariableName(currentChar);

                //Ignore name if it is in black list
                if (!blackList.Contains(name))
                {
                    var immutableArray = semanticModel.LookupSymbols(scope.SpanStart, name: name);

                    if (immutableArray.Length == 0)
                    {
                        return name;
                    }

                    if (immutableArray.All(symbol => symbol.Kind != SymbolKind.Local && symbol.Kind != SymbolKind.Parameter))
                    {
                        return name;
                    }
                }

                currentChar++;
            } while (true);
        }

        private static string ToVariableName(int number)
        {
            var dividend = number;
            var result = String.Empty;

            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                result = Convert.ToChar('a' + modulo) + result;
                dividend = (dividend - modulo) / 26;
            }

            return result;
        }

        internal static Location GetInvocationLocationWithArguments(this InvocationExpressionSyntax invokeExpression)
        {
            var memberAccess = invokeExpression?.ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();

            var nameSyntax = memberAccess?.Name;

            var sourceSpan = nameSyntax.GetLocation().SourceSpan;
            var location = Location.Create(invokeExpression.SyntaxTree, new TextSpan(sourceSpan.Start, sourceSpan.Length + invokeExpression.ArgumentList.GetLocation().SourceSpan.Length));

            return location;
        }
    }
}