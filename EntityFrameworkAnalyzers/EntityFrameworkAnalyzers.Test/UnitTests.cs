using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

using EntityFrameworkAnalyzers.Test.Helpers;

namespace EntityFrameworkAnalyzers.Test
{
    [TestClass]
    public class UnitTest : EntityFrameworkCodeFixVerifier
    {
        private string sourceWithIssue =
@"namespace ConsoleApplication1
{
    class TypeName
    {   
        public void Test()
        {
            var model = new Model();
            var query = model.Salesmen.Include(""Orders"");
        }                
    }
}";

        private string sourceWithoutIssue =
@"using System.Data.Entity;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Test()
        {
            var model = new Model();
            var query = model.Salesmen.Include(a => a.Orders);
        }
    }
}";

        private string methodWithIssue = @"var model = new Model();
            var query = model.Salesmen.Include(""Orders"");";

        private string methodWithoutIssue = @"var model = new Model();
            var query = model.Salesmen.Include({0} => {0}.Orders);";

        [TestMethod]
        public void IncludeMethodWithStringGeneratesDiagnostic()
        {
            var expected = new DiagnosticResult
            {
                Id = Diagnostics.UseIncludeWithLambdaAnalyzerDiagnosticId,
                Message = string.Format(Resources.IncludeLambdaAnalyzerMessageFormat, "Orders"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 40) }
            };

            var source = methodWithIssue.AddMethodAndClass();

            VerifyCSharpDiagnostic(source, expected);
        }

        [TestMethod]
        public void ChangesIncludeMethodWithStringToLambda()
        {
            VerifyCSharpFix(methodWithIssue.AddMethodAndClass(), methodWithoutIssue.FormatWith("a").AddMethodAndClass("System.Data.Entity"));
        }

        [TestMethod]
        public void IncludeMethodWithLambdaDoesNotGenerateDiagnostic()
        {
            VerifyCSharpDiagnostic(methodWithoutIssue.FormatWith("a").AddMethodAndClass());
        }

        [TestMethod]
        public void ChangesIncludeMethodWithStringToLambdaAndDoesNotUseLocalVariableName()
        {
            var methodWithIssueAndLocalVariable = $"var a = 1;{Environment.NewLine}{methodWithIssue}";
            var methodWithoutIssueAndLocalVariable = $"var a = 1;{Environment.NewLine}{methodWithoutIssue}";

            var newSource = methodWithoutIssueAndLocalVariable.FormatWith("b").AddMethodAndClass("System.Data.Entity");
            VerifyCSharpFix(methodWithIssueAndLocalVariable.AddMethodAndClass(), FormatSource(newSource));
        }

        [TestMethod]
        public void ChangesIncludeMethodWithStringToLambdaAndDoesNotUseMethodVariableName()
        {
            var methodWithIssueAndLocalVariable = $"public void Test(int a){Environment.NewLine}{{{methodWithIssue}}}";
            var methodWithoutIssueAndLocalVariable = $"public void Test(int a){Environment.NewLine}{{{{{methodWithoutIssue}}}}}";

            var oldSource = methodWithIssueAndLocalVariable.AddClass();
            var newSource = methodWithoutIssueAndLocalVariable.FormatWith("b").AddClass("System.Data.Entity");

            VerifyCSharpFix(FormatSource(oldSource), FormatSource(newSource));
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new UseIncludeWithLambdaAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UseIncludeWithLambdaAnalyzer();
        }
    }
}