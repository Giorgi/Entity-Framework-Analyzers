using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;
using EntityFrameworkAnalyzers;
using EntityFrameworkAnalyzers.Test.Helpers;
using static System.String;

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

        [TestMethod]
        public void IncludeMethodWithStringGeneratesWarning()
        {
            var expected = new DiagnosticResult
            {
                Id = Diagnostics.UseIncludeWithLambdaAnalyzerDiagnosticId,
                Message = Format(Resources.IncludeLambdaAnalyzerMessageFormat, "Orders"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 40) }
            };

            VerifyCSharpDiagnostic(sourceWithIssue, expected);
        }

        [TestMethod]
        public void ChangesIncludeMethodWithStringToLambda()
        {
            VerifyCSharpFix(sourceWithIssue, sourceWithoutIssue);
        }
        
        [TestMethod]
        public void IncludeMethodWithLambdaDoesNotGenerateWarning()
        {
            VerifyCSharpDiagnostic(sourceWithoutIssue);
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